
$(function () {

    const MONTHS = ['', 'January', 'February', 'March', 'April', 'May',
        'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    const now = new Date();
    const pkr = v => 'Rs ' + Number(v).toLocaleString('en-PK', { minimumFractionDigits: 0 });

    let salDt = null;
    let currentStaffId = null;

 
    const $yr = $('#selYear');
    for (let y = now.getFullYear(); y >= now.getFullYear() - 3; y--)
        $yr.append(`<option value="${y}">${y}</option>`);

    $('#selMonth').val(now.getMonth() + 1);
    $yr.val(now.getFullYear());

    function loadDashboard() {
        const m = +$('#selMonth').val();
        const y = +$('#selYear').val();

        $('#tblLoader').show();

        $.getJSON(`/Salary/GetSummary?month=${m}&year=${y}`, res => {
            if (!res.success) return;
            const d = res.data;
            $('#sumEmp').text(d.totalEmployees);
            $('#sumPayroll').text(pkr(d.totalPayroll));
            $('#sumPaid').text(pkr(d.totalPaid));
            $('#sumPending').text(pkr(d.totalPending));
            $('#sumCounts').text(`${d.paidCount} / ${d.partialCount} / ${d.pendingCount}`);
        });

        $.getJSON(`/Salary/GetDashboard?month=${m}&year=${y}`, res => {
            $('#tblLoader').hide();
            if (!res.success) { gnsToast(res.message || 'Load failed.', 'error'); return; }
            buildTable(res.data, m, y);
        }).fail(() => { $('#tblLoader').hide(); gnsToast('Server error.', 'error'); });
    }

    function buildTable(data, m, y) {
        if (salDt) { salDt.destroy(); salDt = null; }

        salDt = $('#salTable').DataTable({
            data,
            destroy: true,
            pageLength: 25,
            order: [[5, 'desc']],
            columns: [

                {
                    data: 'fullName',
                    render: (v, _, r) =>
                        `<a href="/Salary/PaymentHistory?staffId=${r.staffId}"
                            style="color:var(--gns-accent);font-weight:700;text-decoration:none">
                            ${v}</a>`
                },
                { data: 'department', defaultContent: '—' },

                // Basic salary
                {
                    data: 'basicSalary',
                    render: v => `<span style="font-family:Syne,sans-serif">${pkr(v)}</span>`
                },

                // Net salary
                {
                    data: 'netSalary',
                    render: (v, _, r) => r.payrollStatus === 'NotGenerated'
                        ? `<span style="color:var(--gns-text-muted);font-size:.78rem">—</span>`
                        : `<span style="font-family:Syne,sans-serif;font-weight:800">${pkr(v)}</span>`
                },

                // Paid this month
                {
                    data: 'paidThisMonth',
                    render: v => `<span style="color:#22c55e;font-family:Syne,sans-serif">${pkr(v)}</span>`
                },

                // Balance — RED / BLUE / GREEN
                {
                    data: 'balanceThisMonth',
                    render: (v) => {
                        if (v > 0) return `<span class="bal-red">${pkr(v)}</span>`;
                        if (v < 0) return `<span class="bal-green">${pkr(Math.abs(v))} <small>(Overpaid)</small></span>`;
                        return `<span class="bal-blue">${pkr(0)}</span>`;
                    }
                },

                // Payroll status badge
                {
                    data: 'payrollStatus',
                    render: v => {
                        const map = {
                            NotGenerated: ['sbadge-none', 'Not Generated'],
                            Pending: ['sbadge-pending', 'Pending'],
                            Partial: ['sbadge-partial', 'Partial'],
                            Paid: ['sbadge-paid', 'Paid'],
                        };
                        const [cls, lbl] = map[v] || ['sbadge-none', v];
                        return `<span class="sbadge ${cls}">${lbl}</span>`;
                    }
                },

                // Total outstanding all months
                {
                    data: 'totalOutstanding',
                    render: v => v > 0
                        ? `<span class="bal-red">${pkr(v)}</span>`
                        : `<span class="bal-blue">${pkr(0)}</span>`
                },

                // Actions
                {
                    data: null, orderable: false,
                    render: r => {
                        const genBtn = r.payrollStatus === 'NotGenerated'
                            ? `<button class="tbl-btn tbl-btn-edit" title="Generate Payroll"
                                onclick="genPayroll(${r.staffId},${m},${y})"
                                style="background:rgba(129,140,248,.1);color:#818cf8">
                                <i class="fas fa-wand-magic-sparkles"></i></button>`
                            : '';
                        return `
                        <div class="tbl-actions">
                            ${genBtn}
                            <button class="tbl-btn tbl-btn-edit" title="Pay Salary"
                                onclick="openPayModal(${r.staffId})"
                                style="background:rgba(34,197,94,.1);color:#22c55e">
                                <i class="fas fa-paper-plane"></i>
                            </button>
                            <a class="tbl-btn tbl-btn-edit" title="Payment History"
                                href="/Salary/PaymentHistory?staffId=${r.staffId}"
                                style="background:rgba(245,166,35,.1);color:var(--gns-accent)">
                                <i class="fas fa-clock-rotate-left"></i>
                            </a>
                        </div>`;
                    }
                }
            ]
        });
    }

    // ── Generate single payroll ───────────────────────────────────
    window.genPayroll = function (staffId, m, y) {
        gnsConfirm(`Generate payroll for ${MONTHS[m]} ${y}?`, () => {
            $.ajax({
                url: '/Salary/GeneratePayroll',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    staffId, month: m, year: y,
                    overtimeRatePerHour: 150, modifiedBy: 'Admin'
                }),
                success: res => {
                    if (!res.success) { gnsToast(res.message, 'error'); return; }
                    gnsToast(`Payroll generated — Net: ${pkr(res.data.netSalary)}`, 'success');
                    loadDashboard();
                },
                error: xhr => gnsToast(xhr.responseJSON?.message || 'Failed.', 'error')
            });
        });
    };

    // ── Generate ALL for current month ───────────────────────────
    $('#btnGenAll').on('click', function () {
        const m = +$('#selMonth').val();
        const y = +$('#selYear').val();
        gnsConfirm(`Generate payroll for ALL staff — ${MONTHS[m]} ${y}?`, () => {
            const rows = salDt?.data().toArray() || [];
            const needed = rows.filter(r => r.payrollStatus === 'NotGenerated');
            if (!needed.length) { gnsToast('All payrolls already generated.', 'success'); return; }

            let done = 0;
            needed.forEach(r => {
                $.ajax({
                    url: '/Salary/GeneratePayroll', method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ staffId: r.staffId, month: m, year: y, overtimeRatePerHour: 150, modifiedBy: 'Admin' }),
                    complete: () => { if (++done === needed.length) { gnsToast(`${needed.length} payrolls generated!`, 'success'); loadDashboard(); } }
                });
            });
        });
    });

    // ── Open Pay Modal ────────────────────────────────────────────
    window.openPayModal = function (staffId) {
        currentStaffId = staffId;

        // Reset fields
        $('#payAmt').val('');
        $('#payDate').val(new Date().toISOString().substring(0, 10));
        $('#payMethod').val('Cash');
        $('#payType').val('Full');
        $('#payReason,#payDesc').val('');
        $('#payEmpName,#payEmpDept,#payEmpBasic,#payEmpOut').text('—');
        $('#pendingMonths').html('<div style="text-align:center;padding:12px;color:var(--gns-text-muted)"><i class="fas fa-spinner fa-spin"></i></div>');

        // Load balance from server
        $.getJSON(`/Salary/GetBalance?staffId=${staffId}`, res => {
            if (!res.success) { gnsToast(res.message, 'error'); return; }
            const d = res.data;

            $('#payEmpName').text(d.fullName);
            $('#payEmpDept').text(d.department || '—');
            $('#payEmpBasic').text(pkr(d.basicSalary));
            $('#payEmpOut').text(pkr(d.totalOutstanding));

            // Pending months list
            const pending = d.payrollBreakdown || [];
            if (!pending.length) {
                $('#pendingMonths').html(`
                    <div style="text-align:center;padding:12px;color:#22c55e">
                        <i class="fas fa-circle-check mr-1"></i>No pending salary.
                    </div>`);
                $('#payAmt').val(0);
            } else {
                const html = pending.map(p => `
                    <div class="pm-row">
                        <span style="font-weight:700">${MONTHS[p.payMonth]} ${p.payYear}</span>
                        <span style="color:var(--gns-text-muted)">Net: ${pkr(p.netSalary)}</span>
                        <span style="color:#22c55e">Paid: ${pkr(p.totalPaid)}</span>
                        <span class="pm-bal"><i class="fas fa-circle-exclamation mr-1"></i>${pkr(p.balance)}</span>
                    </div>`).join('');
                $('#pendingMonths').html(html);
                // Pre-fill with total outstanding
                $('#payAmt').val(d.totalOutstanding);
            }
        });

        $('#payModal').modal('show');
    };

    // ── Confirm payment ───────────────────────────────────────────
    $('#btnConfirmPay').on('click', function () {
        if (!currentStaffId) return;
        const amt = parseFloat($('#payAmt').val());
        if (isNaN(amt) || amt <= 0) { gnsToast('Enter a valid amount.', 'error'); return; }
        if (!$('#payDate').val()) { gnsToast('Payment date required.', 'error'); return; }

        const payload = {
            staffId: currentStaffId,
            amount: amt,
            paymentDate: $('#payDate').val(),
            paymentMethod: $('#payMethod').val(),
            paymentType: $('#payType').val(),
            reason: $('#payReason').val() || null,
            description: $('#payDesc').val() || null,
            createdBy: 'Admin'
        };

        $(this).prop('disabled', true)
            .html('<i class="fas fa-spinner fa-spin"></i> Processing…');

        $.ajax({
            url: '/Salary/Pay', method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: res => {
                if (!res.success) { gnsToast(res.message, 'error'); return; }
                $('#payModal').modal('hide');
                gnsToast('Payment recorded successfully!', 'success');
                loadDashboard();
            },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Payment failed.', 'error'),
            complete: () =>
                $('#btnConfirmPay').prop('disabled', false)
                    .html('<i class="fas fa-paper-plane"></i> Confirm Payment')
        });
    });

    $('#btnLoad').on('click', loadDashboard);
    loadDashboard();
});