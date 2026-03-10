/* ═══════════════════════════════════════════════════════════════
   shopExpenses.js  —  calls MVC /ShopExpenses/* controller
   ═══════════════════════════════════════════════════════════════ */
$(function () {
    const CTRL = '/ShopExpenses';
    let dt;
    const now = new Date();

    function initTable(data) {
        if (dt) dt.destroy();
        dt = $('#shopExpTable').DataTable({
            data, destroy: true, pageLength: 10, order: [[0, 'desc']],
            columns: [
                { data: 'expenseId' },
                { data: 'title' },
                { data: 'category', defaultContent: '—' },
                {
                    data: 'amount',
                    render: v => `<span style="font-family:Syne,sans-serif;font-weight:700;color:var(--gns-accent)">Rs ${Number(v).toLocaleString()}</span>`
                },
                { data: 'expenseDate', render: v => v ? v.substring(0, 10) : '—' },
                {
                    data: 'isActive',
                    render: v => v
                        ? '<span class="gns-badge gns-badge-active">Active</span>'
                        : '<span class="gns-badge gns-badge-inactive">Inactive</span>'
                },
                {
                    data: 'expenseId', orderable: false,
                    render: id => `<div class="tbl-actions">
                    <button class="tbl-btn tbl-btn-edit"   onclick="seEdit(${id})"   title="Edit"><i class="fas fa-pen"></i></button>
                    <button class="tbl-btn tbl-btn-delete" onclick="seDelete(${id})" title="Delete"><i class="fas fa-trash"></i></button>
                  </div>` }
            ]
        });

        $('#seStatTotal').text(data.length);
        const total = data.reduce((a, x) => a + (x.amount || 0), 0);
        $('#seStatSum').text('Rs ' + Number(total).toLocaleString());
        const month = data.filter(x => {
            const d = new Date(x.expenseDate);
            return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
        }).reduce((a, x) => a + (x.amount || 0), 0);
        $('#seStatMonth').text('Rs ' + Number(month).toLocaleString());
    }

    function load() {
        $('#seTblLoader').addClass('show');
        $.ajax({
            url: `${CTRL}/GetAll`, method: 'GET',
            success: data => { initTable(data); $('#seTblLoader').removeClass('show'); },
            error: xhr => { gnsToast(xhr.responseJSON?.message || 'Load failed.', 'error'); $('#seTblLoader').removeClass('show'); }
        });
    }

    $('#btnAddShopExp').on('click', () => {
        $('#seId').val(''); $('#seModalTitle').text('Add Shop Expense');
        $('#seTitle,#seCategory,#seAmount,#seDesc').val('');
        $('#seDate').val(''); $('#seIsActive').val('true');
        $('#shopExpModal').modal('show');
    });

    window.seEdit = function (id) {
        $.ajax({
            url: `${CTRL}/GetById`, method: 'GET', data: { id },
            success: s => {
                $('#seId').val(s.expenseId); $('#seModalTitle').text('Edit Expense');
                $('#seTitle').val(s.title); $('#seCategory').val(s.category || '');
                $('#seAmount').val(s.amount);
                $('#seDate').val(s.expenseDate ? s.expenseDate.substring(0, 10) : '');
                $('#seDesc').val(s.description || '');
                $('#seIsActive').val(s.isActive ? 'true' : 'false');
                $('#shopExpModal').modal('show');
            },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Failed to load.', 'error')
        });
    };

    $('#seSave').on('click', function () {
        const t = $('#seTitle').val().trim(), a = $('#seAmount').val(), d = $('#seDate').val();
        if (!t) { gnsToast('Title required.', 'error'); return; }
        if (!a) { gnsToast('Amount required.', 'error'); return; }
        if (!d) { gnsToast('Date required.', 'error'); return; }

        const payload = {
            expenseId: parseInt($('#seId').val()) || 0,
            title: t, category: $('#seCategory').val() || null,
            amount: parseFloat(a), expenseDate: d,
            description: $('#seDesc').val() || null,
            isActive: $('#seIsActive').val() === 'true', modifiedBy: 'Admin'
        };
        const id = $('#seId').val();
        $(this).prop('disabled', true);
        $.ajax({
            url: id ? `${CTRL}/Update?id=${id}` : `${CTRL}/Insert`,
            method: id ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: res => { $('#shopExpModal').modal('hide'); gnsToast(res.message, 'success'); load(); },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Save failed.', 'error'),
            complete: () => $('#seSave').prop('disabled', false)
        });
    });

    window.seDelete = function (id) {
        gnsConfirm('Delete this shop expense?', () => {
            $.ajax({
                url: `${CTRL}/Delete?id=${id}&modifiedBy=Admin`, method: 'DELETE',
                success: res => { gnsToast(res.message, 'success'); load(); },
                error: xhr => gnsToast(xhr.responseJSON?.message || 'Delete failed.', 'error')
            });
        });
    };

    $('#seRefresh').on('click', load);
    load();
});