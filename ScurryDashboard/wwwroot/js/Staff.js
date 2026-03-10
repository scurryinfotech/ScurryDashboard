$(function () {

    const CTRL = '/Staff';
    let dt;

    function loadRoles() {
        $.getJSON('/Roles/GetAll', function (data) {
            const $sel = $('#sfRole').find('option:not(:first)').remove().end();
            $.each(data, function (_, r) {
                $sel.append(`<option value="${r.roleId}">${r.roleName}</option>`);
            });
        });
    }

    function initTable(data) {
        if (dt) dt.destroy();
        dt = $('#staffTable').DataTable({
            data, destroy: true, pageLength: 10, order: [[0, 'desc']],
            columns: [
                { data: 'staffId' },
                {
                    data: 'fullName',
                    render: (v, _, row) =>
                        `<a href='/Staff/Profile/${row.staffId}'
                            style='color:var(--gns-accent);font-weight:600;text-decoration:none'>${v}</a>`
                },
                { data: 'roleName', defaultContent: '—' },
                { data: 'department', defaultContent: '—' },
                { data: 'phone', defaultContent: '—' },
                {
                    data: 'salary',
                    render: v =>
                        `<span style="font-family:Syne,sans-serif;font-weight:700;color:var(--gns-accent)">
                            Rs ${Number(v).toLocaleString()}
                        </span>`
                },
                {
                    data: 'isActive',
                    render: v => v
                        ? '<span class="gns-badge gns-badge-active"><i class="fas fa-circle" style="font-size:.45rem;margin-right:4px"></i>Active</span>'
                        : '<span class="gns-badge gns-badge-inactive"><i class="fas fa-circle" style="font-size:.45rem;margin-right:4px"></i>Inactive</span>'
                },
                {
                    data: 'staffId', orderable: false,
                    render: id => `
                        <div class="tbl-actions">
                            <button class="tbl-btn tbl-btn-edit"
                                onclick="editStaff(${id})" title="Edit">
                                <i class="fas fa-pen"></i>
                            </button>
                            <button class="tbl-btn tbl-btn-delete"
                                onclick="deleteStaff(${id})" title="Delete">
                                <i class="fas fa-trash"></i>
                            </button>
                            <a class="tbl-btn tbl-btn-edit"
                                href="/Salary/PaymentHistory?staffId=${id}"
                                title="Payment History"
                                style="background:rgba(129,140,248,.1);color:#818cf8">
                                <i class="fas fa-clock-rotate-left"></i>
                            </a>
                            <a class="tbl-btn tbl-btn-edit"
                                href="/Salary/Index"
                                title="Salary Dashboard"
                                style="background:rgba(34,197,94,.1);color:#22c55e">
                                <i class="fas fa-money-check-dollar"></i>
                            </a>
                        </div>`
                }
            ]
        });

        const active = data.filter(s => s.isActive).length;
        const salary = data.filter(s => s.isActive).reduce((a, s) => a + (s.salary || 0), 0);
        $('#statTotal').text(data.length);
        $('#statActive').text(active);
        $('#statSalary').text('Rs ' + Number(salary).toLocaleString());
    }

    // ── GET /Staff/GetAll ─────────────────────────────────────
    function loadStaff() {
        $('#tblLoader').addClass('show');
        $.ajax({
            url: `${CTRL}/GetAll`, method: 'GET',
            success: data => {
                initTable(data);
                $('#tblLoader').removeClass('show');
            },
            error: xhr => {
                gnsToast(xhr.responseJSON?.message || 'Load failed.', 'error');
                $('#tblLoader').removeClass('show');
            }
        });
    }

    // ── Open Add modal ────────────────────────────────────────
    $('#btnAddStaff').on('click', function () {
        $('#staffId').val('');
        $('#staffModalTitle').text('Add Staff');
        $('#sfFullName,#sfPhone,#sfEmail,#sfCNIC,#sfDept,#sfSalary,#sfJoinDate').val('');
        $('#sfRole').val('');
        $('#sfIsActive').val('true');
        $('#staffModal').modal('show');
    });

    // ── GET /Staff/GetById?id=5 ───────────────────────────────
    window.editStaff = function (id) {
        $.ajax({
            url: `${CTRL}/GetById`, method: 'GET', data: { id },
            success: s => {
                $('#staffId').val(s.staffId);
                $('#staffModalTitle').text('Edit Staff');
                $('#sfFullName').val(s.fullName);
                $('#sfRole').val(s.roleId);
                $('#sfPhone').val(s.phone);
                $('#sfEmail').val(s.email);
                $('#sfCNIC').val(s.cnic);
                $('#sfDept').val(s.department);
                $('#sfSalary').val(s.salary);
                $('#sfJoinDate').val(s.joinDate ? s.joinDate.substring(0, 10) : '');
                $('#sfIsActive').val(s.isActive ? 'true' : 'false');
                $('#staffModal').modal('show');
            },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Failed to load.', 'error')
        });
    };

    // ── POST /Staff/Insert  or  PUT /Staff/Update?id=5 ────────
    $('#btnSaveStaff').on('click', function () {
        const name = $('#sfFullName').val().trim();
        const role = $('#sfRole').val();
        if (!name) { gnsToast('Full name is required.', 'error'); return; }
        if (!role) { gnsToast('Please select a role.', 'error'); return; }

        const payload = {
            staffId: parseInt($('#staffId').val()) || 0,
            fullName: name,
            roleId: parseInt(role),
            phone: $('#sfPhone').val() || null,
            email: $('#sfEmail').val() || null,
            cnic: $('#sfCNIC').val() || null,
            department: $('#sfDept').val() || null,
            salary: parseFloat($('#sfSalary').val()) || 0,
            joinDate: $('#sfJoinDate').val() || null,
            isActive: $('#sfIsActive').val() === 'true',
            modifiedBy: 'Admin'
        };

        const id = $('#staffId').val();
        const url = id ? `${CTRL}/Update?id=${id}` : `${CTRL}/Insert`;
        const method = id ? 'PUT' : 'POST';

        $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Saving…');

        $.ajax({
            url, method,
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: res => {
                $('#staffModal').modal('hide');
                gnsToast(res.message || 'Saved!', 'success');
                loadStaff();
            },
            error: xhr => gnsToast(xhr.responseJSON?.message || 'Save failed.', 'error'),
            complete: () => $('#btnSaveStaff')
                .prop('disabled', false)
                .html('<i class="fas fa-floppy-disk"></i> Save')
        });
    });

  
    window.deleteStaff = function (id) {
        gnsConfirm('Delete this staff member?', function () {
            $.ajax({
                url: `${CTRL}/Delete?id=${id}&modifiedBy=Admin`, method: 'DELETE',
                success: res => { gnsToast(res.message, 'success'); loadStaff(); },
                error: xhr => gnsToast(xhr.responseJSON?.message || 'Delete failed.', 'error')
            });
        });
    };

    $('#btnRefresh').on('click', loadStaff);

    loadRoles();
    loadStaff();
});