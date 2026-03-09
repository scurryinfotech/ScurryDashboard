/* ═══════════════════════════════════════════════════════════════
   GNS — site.js  (global helpers + sidebar + toast + confirm)
   ═══════════════════════════════════════════════════════════════ */

// ── Config ────────────────────────────────────────────────────
const GNS_API = window.GNS_CONFIG?.apiBase || "https://localhost:7001";

// ── Sidebar toggle ────────────────────────────────────────────
$(function () {
    const toggle = $('#sidebarToggle');
    const isMobile = () => window.innerWidth <= 768;

    toggle.on('click', function () {
        if (isMobile()) {
            $('.gns-sidebar').toggleClass('mobile-open');
        } else {
            $('body').toggleClass('sidebar-collapsed');
            localStorage.setItem('gns_sidebar', $('body').hasClass('sidebar-collapsed') ? '1' : '0');
        }
    });

    // Restore state
    if (!isMobile() && localStorage.getItem('gns_sidebar') === '1') {
        $('body').addClass('sidebar-collapsed');
    }

    // Close on outside click (mobile)
    $(document).on('click', function (e) {
        if (isMobile() && !$(e.target).closest('.gns-sidebar, #sidebarToggle').length) {
            $('.gns-sidebar').removeClass('mobile-open');
        }
    });
});

// ── Toast ─────────────────────────────────────────────────────
function gnsToast(msg, type = 'info', duration = 3500) {
    const icons = { success: 'fa-circle-check', error: 'fa-circle-xmark', info: 'fa-circle-info' };
    const $t = $(`<div class="gns-toast ${type}">
        <i class="fas ${icons[type] || icons.info}"></i>
        <span class="gns-toast__msg">${msg}</span>
    </div>`);
    $('#toastContainer').append($t);
    setTimeout(() => $t.fadeOut(300, function () { $(this).remove(); }), duration);
}

$(function () {
    if (!$('#confirmModal').length) {
        $('body').append(`
        <div class="modal fade" id="confirmModal" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-sm">
            <div class="modal-content">
              <div class="modal-body">
                <i class="fas fa-triangle-exclamation"></i>
                <p id="confirmModalMsg">Are you sure?</p>
              </div>
              <div class="modal-footer justify-content-center">
                <button type="button" class="btn-gns btn-gns-ghost" data-dismiss="modal">Cancel</button>
                <button type="button" class="btn-gns btn-gns-danger" id="confirmModalYes">Delete</button>
              </div>
            </div>
          </div>
        </div>`);
    }
});

function gnsConfirm(msg, onYes) {
    $('#confirmModalMsg').text(msg);
    $('#confirmModal').modal('show');
    $('#confirmModalYes').off('click').on('click', function () {
        $('#confirmModal').modal('hide');
        onYes();
    });
}

function gnsAjax({ url, method = 'GET', data = null, success, fail }) {
    const opts = {
        url, method,
        contentType: 'application/json',
        success: function (res) {
            if (res && res.success === false) {
                gnsToast(res.message || 'Request failed.', 'error');
                if (fail) fail(res);
            } else {
                if (success) success(res);
            }
        },
        error: function (xhr) {
            const msg = xhr.responseJSON?.message || 'Server error.';
            gnsToast(msg, 'error');
            if (fail) fail(xhr);
        }
    };
    if (data) opts.data = JSON.stringify(data);
    $.ajax(opts);
}

$(function () {
    const btn = $('#HomeBtn');
    const status = $('#HomeStatus');
    if (!btn.length) return;

    btn.on('click', function () {
        const connected = btn.data('state') === 'connected';
        btn.data('state', connected ? 'disconnected' : 'connected');
        btn.attr('data-state', connected ? 'disconnected' : 'connected');
        status.text(connected ? 'Disconnected' : 'Connected');
        gnsToast(connected ? 'Home Delivery disconnected.' : 'Home Delivery connected!',
            connected ? 'error' : 'success');
    });
});