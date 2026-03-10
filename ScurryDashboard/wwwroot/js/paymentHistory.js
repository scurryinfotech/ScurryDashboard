
$(function () {

    const MONTHS = ['', 'January', 'February', 'March', 'April', 'May',
        'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    const pkr = v => 'Rs ' + Number(v).toLocaleString('en-PK', { minimumFractionDigits: 0 });
    const STAFF_ID = window.HISTORY_STAFF_ID;

    let txnDt = null;


    function load() {

        $.getJSON(`/Salary/GetHistory?staffId=${STAFF_ID}`, res => {
            if (!res.success) { gnsToast(res.message || 'Load failed.', 'error'); return; }
            renderPayrollCards(res.data.payrolls || []);
            renderTransactions(res.data.payments || []);
        });


        $.getJSON(`/Salary/GetBalance?staffId=${STAFF_ID}`, res => {
            if (!res.success) return;
            const s = res.data;
            const ini = (s.fullName || '?').split(' ')
                .map(w => w[0]).join('').substring(0, 2).toUpperCase();
            $('#phAvatar').text(ini);
            $('#phName').text(s.fullName);
            $('#phDept').text(s.department || '—');
            $('#phBasic').text(pkr(s.basicSalary));
            $('#phOut').text(pkr(s.totalOutstanding));
            document.title = s.fullName + ' — Payment History';
        });
    }

    function renderPayrollCards(payrolls) {
        if (!payrolls.length) {
            $('#pmCards').html(`
                <div style="text-align:center;padding:24px;color:var(--gns-text-muted)">
                    No payroll records found.
                </div>`);
            return;
        }

        const html = payrolls.map(p => {
            const bal = p.balance;
            const balCls = bal > 0 ? 'bal-red' : bal < 0 ? 'bal-green' : 'bal-blue';
            const balLbl = bal > 0 ? 'Pending' : bal < 0 ? 'Overpaid' : 'Fully Paid';
            const stMap = { Pending: 'sbadge-pending', Partial: 'sbadge-partial', Paid: 'sbadge-paid' };
            const stCls = stMap[p.payrollStatus] || 'sbadge-none';

            return `
            <div class="pm-card">
                <div class="pm-card__month">
                    ${MONTHS[p.payMonth]} ${p.payYear}
                    <div style="margin-top:5px">
                        <span class="sbadge ${stCls}">${p.payrollStatus}</span>
                    </div>
                </div>
                <div class="pm-card__figs">
                    <div class="pm-fig">
                        <div class="pm-fig-val" style="color:var(--gns-accent)">${pkr(p.basicSalary)}</div>
                        <div class="pm-fig-lbl">Basic</div>
                    </div>
                    <div class="pm-fig">
                        <div class="pm-fig-val" style="color:var(--gns-text)">${pkr(p.netSalary)}</div>
                        <div class="pm-fig-lbl">Net Salary</div>
                    </div>
                    <div class="pm-fig">
                        <div class="pm-fig-val" style="color:#22c55e">${pkr(p.totalPaid)}</div>
                        <div class="pm-fig-lbl">Total Paid</div>
                    </div>
                    ${p.overtimeAmount > 0 ? `
                    <div class="pm-fig">
                        <div class="pm-fig-val" style="color:#818cf8">${pkr(p.overtimeAmount)}</div>
                        <div class="pm-fig-lbl">OT Amount</div>
                    </div>` : ''}
                    ${p.deductions > 0 ? `
                    <div class="pm-fig">
                        <div class="pm-fig-val" style="color:#ef4444">-${pkr(p.deductions)}</div>
                        <div class="pm-fig-lbl">Deductions</div>
                    </div>` : ''}
                </div>
                <div class="pm-card__bal">
                    <div class="pm-bal-val ${balCls}">${pkr(Math.abs(bal))}</div>
                    <div class="pm-bal-lbl">${balLbl}</div>
                </div>
            </div>`;
        }).join('');

        $('#pmCards').html(html);
    }

    function renderTransactions(payments) {
        if (txnDt) { txnDt.destroy(); txnDt = null; }

        txnDt = $('#txnTable').DataTable({
            data: payments,
            destroy: true,
            pageLength: 15,
            order: [[1, 'desc']],
            columns: [
                { data: 'paymentId' },

                {
                    data: 'paymentDate',
                    render: v => `<span style="font-family:Syne,sans-serif;font-weight:700">${v}</span>`
                },

                {
                    data: null,
                    render: r => r.payMonth
                        ? `${MONTHS[r.payMonth]} ${r.payYear}`
                        : `<span style="color:var(--gns-text-muted)">Advance</span>`
                },

                {
                    data: 'amount',
                    render: v => `<span style="font-family:Syne,sans-serif;font-weight:800;
                                       color:var(--gns-accent)">${pkr(v)}</span>`
                },

                { data: 'paymentMethod', defaultContent: '—' },

                {
                    data: 'paymentType',
                    render: v => {
                        const cls = {
                            Advance: 'tbadge-advance',
                            Partial: 'tbadge-partial',
                            Full: 'tbadge-full'
                        }[v] || '';
                        return `<span class="${cls}">${v}</span>`;
                    }
                },

                { data: 'reason', defaultContent: '—' },
                { data: 'description', defaultContent: '—' },
            ]
        });
    }

    load();
});