/* dailyExpenses.js — with PaymentMode */
$(function () {
    const CTRL = '/DailyExpenses';
    let dt;
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const weekAgo = new Date(today); weekAgo.setDate(weekAgo.getDate() - 6);

    const modeIcon = {
        'Cash': '<i class="fas fa-money-bill-wave" style="color:#22c55e"></i>',
        'Online': '<i class="fas fa-mobile-alt"      style="color:#818cf8"></i>',
        'Cheque': '<i class="fas fa-money-check"     style="color:#f59e0b"></i>',
        'Bank Transfer': '<i class="fas fa-university"      style="color:#22d3ee"></i>'
    };

    function initTable(data) {
        if (dt) dt.destroy();
        dt = $('#dailyExpTable').DataTable({
            data, destroy: true, pageLength: 10, order: [[0, 'desc']],
            columns: [
                { data: 'dailyExpenseId' },
                { data: 'title' },
                { data: 'category', defaultContent: '—' },
                {
                    data: 'amount',
                    render: v => `<span style="font-family:Syne,sans-serif;font-weight:700;color:var(--gns-accent)">Rs ${Number(v).toLocaleString()}</span>`
                },
                { data: 'expenseDate', render: v => v ? v.substring(0, 10) : '—' },
                {
                    data: 'paymentMode',
                    defaultContent: 'Cash',
                    render: v => `<span style="display:flex;align-items:center;gap:5px">
                        ${modeIcon[v] || ''} ${v || 'Cash'}
                    </span>`
                },
                { data: 'paidBy', defaultContent: '—' },
                {
                    data: 'isActive',
                    render: v => v
                        ? '<span class="gns-badge gns-badge-active">Active</span>'
                        : '<span class="gns-badge gns-badge-inactive">Inactive</span>'
                },
                {
                    data: 'dailyExpenseId', orderable: false,
                    render: id => `<div class="tbl-actions">
                        <button class="tbl-btn tbl-btn-edit"   onclick="deEdit(${id})"   title="Edit"><i class="fas fa-pen"></i></button>
                        <button class="tbl-btn tbl-btn-delete" onclick="deDelete(${id})" title="Delete"><i class="fas fa-trash"></i></button>
                    </div>`
                }
            ]
        });

        $('#deStatTotal').text(data.length);
        const todayAmt = data.filter(x => {
            const d = new Date(x.expenseDate); d.setHours(0, 0, 0, 0);
            return d.getTime() === today.getTime();
        }).reduce((a, x) => a + (x.amount || 0), 0);
        const weekAmt = data.filter(x => {
            const d = new Date(x.expenseDate);
            return d >= weekAgo && d <= today;
        }).reduce((a, x) => a + (x.amount || 0), 0);
        $('#deStatToday').text('Rs ' + Number(todayAmt).toLocaleString());
        $('#deStatWeek').text('Rs ' + Number(weekAmt).toLocaleString());
    }

    function load() {
        $('#deTblLoader').addClass('show');
        $.ajax({
            url: `${CTRL}/GetAll`, method: 'GET',
            success: data => { initTable(data); $('#deTblLoader').removeClass('show'); },
            error: xhr => { gnsToast(xhr.responseJSON?.message || 'Load failed.', 'error'); $('#deTblLoader').removeClass('show'); }
        });
    }

    $('#btnAddDE').on('click', () => {
        $('#deId').val('');
        $('#deModalTitle').text('Add Daily Expense');
        $('#deTitle,#deCategory,#deAmount,#dePaidBy,#deNotes').val('');
        $('#deDate').val(new Date().toISOString().substring(0, 10));
        $('#dePaymentMode').val('Cash');
        $('#deIsActive').val('true');
        $('#deModal').modal('show');
    });

    window.deEdit = function (id) {
        $.ajax({
            url: `${CTRL}/GetById`, method: 'GET', data: { id },
            success: s => {
                $('#deId').val(s.dailyExpenseId);
                $('#deModalTitle').text('Edit Daily Expense');
                $('#deTitle').val(s.title);
                $('#deCategory').val(s.category || '');
                $('#deAmount').val(s.amount);
                $('#deDate').val(s.expenseDate ? s.expenseDate.substring(0, 10) : '');
                $('#dePaymentMode').val(s.paymentMode || 'Cash');
                $('#dePaidBy').val(s.paidBy || '');
                $('#deNotes').val(s.notes || '');
                $('#deIsActive').val(s.isActive ? 'true' : 'false');
                $('#deModal').modal('show');
            },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Failed to load.', 'error')
        });
    };

    $('#deSave').on('click', function () {
        const t = $('#deTitle').val().trim(), a = $('#deAmount').val(), d = $('#deDate').val();
        if (!t) { gnsToast('Title required.', 'error'); return; }
        if (!a) { gnsToast('Amount required.', 'error'); return; }
        if (!d) { gnsToast('Date required.', 'error'); return; }

        const payload = {
            dailyExpenseId: parseInt($('#deId').val()) || 0,
            title: t,
            category: $('#deCategory').val() || null,
            amount: parseFloat(a),
            expenseDate: d,
            paymentMode: $('#dePaymentMode').val() || 'Cash',
            paidBy: $('#dePaidBy').val() || null,
            notes: $('#deNotes').val() || null,
            isActive: $('#deIsActive').val() === 'true',
            modifiedBy: 'Admin'
        };
        const id = $('#deId').val();
        $(this).prop('disabled', true);
        $.ajax({
            url: id ? `${CTRL}/Update?id=${id}` : `${CTRL}/Insert`,
            method: id ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: res => { $('#deModal').modal('hide'); gnsToast(res.message, 'success'); load(); },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Save failed.', 'error'),
            complete: () => $('#deSave').prop('disabled', false)
        });
    });

    window.deDelete = function (id) {
        gnsConfirm('Delete this daily expense?', () => {
            $.ajax({
                url: `${CTRL}/Delete?id=${id}&modifiedBy=Admin`, method: 'DELETE',
                success: res => { gnsToast(res.message, 'success'); load(); },
                error: xhr => gnsToast(xhr.responseJSON?.message || 'Delete failed.', 'error')
            });
        });
    };

    $('#deRefresh').on('click', load);
    load();
});