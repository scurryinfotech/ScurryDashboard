
var TM_API = {
    tables: '/Home/GetTableCount',
    orders: '/Home/GetOrder',
    save: '/Home/SaveTableOrder',
    bill: '/Home/GetBillData',
};

/* ── State ── */
var _allTables = [];  
var _liveOrders = {};   
var _refreshTimer = null;
var _activeTable = null; 


$(document).ready(function () {
    _loadAll();

    /* Auto-refresh every 30 sec */
    _refreshTimer = setInterval(_loadAll, 30000);

    /* Panel backdrop close */
    $('#tmPanelBg').on('click', function (e) {
        if (e.target === this) closePanel();
    });

    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') closePanel();
    });
});


function _loadAll(showSpinner) {
    if (showSpinner) {
        $('#tmRefreshIcon').addClass('spin');
    }

    $.when(
        $.ajax({ url: TM_API.tables, type: 'GET', dataType: 'json' }),
        $.ajax({ url: TM_API.orders, type: 'GET', dataType: 'json' })
    ).done(function (tRes, oRes) {

        var rawTables = tRes[0];
        var rawOrders = oRes[0];

 
        _allTables = [];
        if ($.isArray(rawTables)) {
            $.each(rawTables, function (_, t) {
                var no = t.tableNo || t.tableNumber || t.TableNo || t.TableNumber || t.table_no || t;
                _allTables.push({ tableNo: parseInt(no) || no });
            });
        } else {
            var cnt = rawTables.tableCount || rawTables.TableCount || rawTables || 20;
            for (var i = 1; i <= cnt; i++) _allTables.push({ tableNo: i });
        }


        _liveOrders = {};
        var orders = $.isArray(rawOrders) ? rawOrders : [];
        $.each(orders, function (_, o) {
            var tno = parseInt(o.tableNo || o.TableNo || o.table_no || o.selectedTable || 0);
            if (!tno) return; // skip phone orders (table 0)
            _liveOrders[tno] = o;
        });

        renderGrid();
        updateStats();

    }).fail(function () {
        tmToast('Could not load table data', 'error');
    }).always(function () {
        $('#tmRefreshIcon').removeClass('spin');
    });
}

function refreshNow() {
    _loadAll(true);
    tmToast('Tables refreshed', 'info');
}


function renderGrid() {
    var $grid = $('#tmGrid');
    if (!_allTables.length) {
        $grid.html('<div style="grid-column:1/-1;text-align:center;padding:40px;color:#aaa;font-size:.8rem">No tables configured</div>');
        return;
    }

    var html = '';
    $.each(_allTables, function (_, t) {
        var tno = t.tableNo;
        var order = _liveOrders[tno];
        var status = order ? _getOrderStatus(order) : 'free';

        html += _buildTableCard(tno, status, order);
    });
    $grid.html(html);
}

function _getOrderStatus(order) {
    var s = (order.orderStatus || order.OrderStatus || order.status || '').toLowerCase();
    if (s === 'billing' || s === 'bill') return 'billing';
    return 'occupied';
}

function _buildTableCard(tno, status, order) {
    var statusLabel = status === 'free' ? 'Free'
        : status === 'billing' ? 'Billing'
            : 'Occupied';

    var infoHtml = '';
    var actionsHtml = '';
    var icon = status === 'free' ? 'fas fa-check-circle' : 'fas fa-users';

    if (order) {
        /* Parse items */
        var items = order.orderItems || order.OrderItems || order.items || [];
        var total = parseFloat(order.totalAmount || order.TotalAmount || order.grandTotal || 0);
        var itemCount = items.length;
        var custName = order.customerName || order.CustomerName || 'Guest';

        /* Duration since order placed */
        var durationHtml = '';
        var orderTime = order.orderTime || order.OrderTime || order.createdDate || order.CreatedDate || null;
        if (orderTime) {
            var mins = Math.floor((Date.now() - new Date(orderTime).getTime()) / 60000);
            var isLong = mins > 60;
            durationHtml = '<span class="tm-duration' + (isLong ? ' long' : '') + '">' +
                (mins < 60 ? mins + ' min' : Math.floor(mins / 60) + 'h ' + (mins % 60) + 'm') +
                '</span>';
        }

        infoHtml = '<div class="tm-table-info">' +
            '<div class="ti-row"><span class="ti-label">Guest</span><span class="ti-val">' + custName + '</span></div>' +
            '<div class="ti-row"><span class="ti-label">Items</span><span class="ti-val">' + itemCount + ' items</span></div>' +
            '<div class="ti-row"><span class="ti-label">Amount</span><span class="ti-amt">&#8377;' + total.toFixed(2) + '</span></div>' +
            (durationHtml ? '<div class="ti-row"><span class="ti-label">Time</span>' + durationHtml + '</div>' : '') +
            '</div>';

        actionsHtml = '<div class="tm-table-actions">' +
            '<button class="tm-tact tm-tact-add"  onclick="event.stopPropagation();addToTable(' + tno + ')">' +
            '<i class="fas fa-plus"></i>Add</button>' +
            '<button class="tm-tact tm-tact-bill" onclick="event.stopPropagation();viewTable(' + tno + ')">' +
            '<i class="fas fa-file-invoice"></i>Bill</button>' +
            '<button class="tm-tact tm-tact-kot"  onclick="event.stopPropagation();viewTable(' + tno + ')">' +
            '<i class="fas fa-receipt"></i>KOT</button>' +
            '<button class="tm-tact tm-tact-free" onclick="event.stopPropagation();markFree(' + tno + ')">' +
            '<i class="fas fa-check"></i>Clear</button>' +
            '</div>';
    } else {
        actionsHtml = '<div class="tm-table-actions">' +
            '<button class="tm-tact tm-tact-add" style="border-right:none" onclick="event.stopPropagation();newOrderForTable(' + tno + ')">' +
            '<i class="fas fa-plus"></i>New Order</button>' +
            '</div>';
    }

    return '<div class="tm-table ' + status + '" onclick="viewTable(' + tno + ')">' +
        '<div class="tm-table-top">' +
        '<div><div class="tm-table-num">T' + tno + '</div><div class="tm-table-label">Table ' + tno + '</div></div>' +
        '<span class="tm-status-badge ' + status + '">' + statusLabel + '</span>' +
        '</div>' +
        '<div class="tm-table-icon"><i class="' + icon + '"></i></div>' +
        infoHtml +
        actionsHtml +
        '</div>';
}


function updateStats() {
    var total = _allTables.length;
    var occupied = Object.keys(_liveOrders).length;
    var free = total - occupied;
    var billing = 0;
    $.each(_liveOrders, function (_, o) {
        if (_getOrderStatus(o) === 'billing') billing++;
    });

    $('#tmStatTotal').text(total);
    $('#tmStatFree').text(free);
    $('#tmStatOccupied').text(occupied - billing);
    $('#tmStatBilling').text(billing);
    $('#tmLastUpdate').text('Last updated: ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
}


/* Open new order page for this table */
function newOrderForTable(tno) {
    window.location.href = '/Home/NewOrder?table=' + tno;
}

/* Add more items to existing table order */
function addToTable(tno) {
    window.location.href = '/Home/NewOrder?table=' + tno;
}

/* Open side panel showing table's current order */
function viewTable(tno) {
    var order = _liveOrders[tno];
    if (!order) {

        _showFreePanel(tno);
        return;
    }
    _activeTable = tno;
    _showOrderPanel(tno, order);
}

function _showFreePanel(tno) {
    _activeTable = tno;
    var html = '<div style="text-align:center;padding:40px 20px;color:#aaa;">' +
        '<i class="fas fa-chair" style="font-size:2.5rem;display:block;margin-bottom:12px;color:#ddd"></i>' +
        '<p style="font-size:.8rem;line-height:1.6">Table ' + tno + ' is <strong style="color:#27ae60">free</strong>.<br>No active orders.</p>' +
        '</div>';
    $('#tmPanelTitle').html('<i class="fas fa-chair"></i> Table ' + tno + ' — Free');
    $('#tmPanelBody').html(html);
    $('#tmPanelFtr').html(
        '<button class="tm-mfbtn tm-mfbtn-add" onclick="newOrderForTable(' + tno + ')">' +
        '<i class="fas fa-plus"></i> Start New Order</button>'
    );
    $('#tmPanelBg').addClass('open');
}

function _showOrderPanel(tno, order) {
    var items = order.orderItems || order.OrderItems || order.items || [];
    var total = parseFloat(order.totalAmount || order.TotalAmount || order.grandTotal || 0);
    var custName = order.customerName || order.CustomerName || 'Guest';
    var phone = order.userPhone || order.phone || order.Phone || '';
    var otype = order.orderType || order.OrderType || 'Dine In';
    var orderId = order.orderId || order.OrderId || order.id || '';
    var orderTime = order.orderTime || order.OrderTime || order.createdDate || '';

    /* Duration */
    var duration = '';
    if (orderTime) {
        var mins = Math.floor((Date.now() - new Date(orderTime).getTime()) / 60000);
        duration = mins < 60 ? mins + ' min' : Math.floor(mins / 60) + 'h ' + (mins % 60) + 'm';
    }

    var metaHtml = '<div class="tm-order-meta">' +
        '<div class="tm-order-meta-row"><span>Order ID</span><span>#' + (orderId || '--') + '</span></div>' +
        '<div class="tm-order-meta-row"><span>Guest</span><span>' + custName + (phone ? ' · ' + phone : '') + '</span></div>' +
        '<div class="tm-order-meta-row"><span>Type</span><span>' + otype + '</span></div>' +
        (duration ? '<div class="tm-order-meta-row"><span>Duration</span><span>' + duration + '</span></div>' : '') +
        '</div>';

    var itemsHtml = '<div class="tm-order-items-title">Order Items</div>';
    if (items.length) {
        $.each(items, function (_, item) {
            var name = item.itemName || item.ItemName || item.name || 'Item';
            var qty = (item.full || item.Full || item.fullPortion || 0) +
                (item.half || item.Half || item.halfPortion || 0);
            var price = parseFloat(item.Price || item.price || 0);
            itemsHtml += '<div class="tm-order-item">' +
                '<span class="tm-oi-name">' + name + '</span>' +
                '<span class="tm-oi-qty">x' + qty + '</span>' +
                '<span class="tm-oi-price">&#8377;' + price.toFixed(2) + '</span>' +
                '</div>';
        });
    } else {
        itemsHtml += '<div style="color:#aaa;font-size:.75rem;padding:12px 0">No item details available</div>';
    }

    var totalHtml = '<div class="tm-order-total">' +
        '<span>Total Amount</span><span>&#8377;' + total.toFixed(2) + '</span>' +
        '</div>';

    $('#tmPanelTitle').html('<i class="fas fa-users"></i> Table ' + tno + ' — Occupied');
    $('#tmPanelBody').html(metaHtml + itemsHtml + totalHtml);
    $('#tmPanelFtr').html(
        '<button class="tm-mfbtn tm-mfbtn-add"  onclick="addToTable(' + tno + ')">' +
        '<i class="fas fa-plus"></i> Add Items</button>' +
        '<button class="tm-mfbtn tm-mfbtn-bill" onclick="genBill(' + tno + ')">' +
        '<i class="fas fa-file-invoice"></i> Generate Bill</button>' +
        '<button class="tm-mfbtn tm-mfbtn-free" onclick="markFree(' + tno + ')">' +
        '<i class="fas fa-check"></i> Clear Table</button>'
    );
    $('#tmPanelBg').addClass('open');
}

function closePanel() {
    $('#tmPanelBg').removeClass('open');
    _activeTable = null;
}

/* Mark table as free (settle / clear order) */
function markFree(tno) {
    if (!confirm('Mark Table ' + tno + ' as free? This will clear the active order.')) return;
    /* POST to close/settle the order */
    var order = _liveOrders[tno];
    if (order) {
        var orderId = order.orderId || order.OrderId || order.id || '';
        if (orderId) {
            $.ajax({
                url: '/Home/SoftDeleteOrder',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ orderId: orderId }),
                complete: function () {
                    delete _liveOrders[tno];
                    closePanel();
                    renderGrid();
                    updateStats();
                    tmToast('Table ' + tno + ' cleared', 'success');
                }
            });
            return;
        }
    }
    delete _liveOrders[tno];
    closePanel();
    renderGrid();
    updateStats();
    tmToast('Table ' + tno + ' is now free', 'success');
}

function genBill(tno) {
    var order = _liveOrders[tno];
    var orderId = order ? (order.orderId || order.OrderId || order.id || '') : '';
    window.location.href = '/Home/NewOrder?table=' + tno + '&bill=1&orderId=' + orderId;
}


function tmToast(msg, type) {
    var colors = { success: '#27ae60', error: '#c0392b', info: '#2980b9' };
    var $t = $('#tmToast');
    $t.css('background', colors[type] || colors.info).html(msg).show();
    clearTimeout($t.data('t'));
    $t.data('t', setTimeout(function () { $t.hide(); }, 3000));
}