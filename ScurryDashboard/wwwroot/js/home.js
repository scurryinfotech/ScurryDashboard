const ALARM_URL = "/sound/alarm.mp3";

let arrTable;
let liveOrdersTable;
let currentTableNo = null;
let orderHistory = [];
let selectedPaymentMode = null;
let prevNewOrderIds = [];

let audioBeep = null;
let isBeepPlaying = false;
let audioUnlocked = false;

let selectedHistoryDate = "";
let selectedHistoryTableNo = "";

let beepTables = {};
let reminderTimers = {};
let orderReceivedTimes = {};
let orderAcceptedTimes = {};
let completionTimers = {};

let paymentContext = null;
let currentOnlineOrderId = null;
let currentOrderId = null;
let currentOrderData = null;

let isHomeActive;
let currentPage = 1;
let pageSize = 20;
let isLoading = false;
let hasMoreData = true;
let allOrdersData = [];
let filteredOrdersData = [];
let selectedDeliveryOrderId = null;

// ========== Utilities ==========

$(document).ready(function () {

    $('#manageLink').show();
    $('#homeLink').hide();
    $('#switchOnOff').show();
    initHomeSwitch();

    setTodayDate();
    setupDiscountButtons();
    setInterval(function () {
        loadTableCount();
        loadTableOrders(false);
        //getOrdersFromRestaurant();
        //setupInfiniteScroll();
        getOrdersFromCoffee();

    }, 3000);

     

    //initializeConnectionUI();

    // Hide New Order button until a table is selected (support several possible selectors)
    try {
        $('[id=newOrderBtn], .new-order-btn, #openNewOrder').hide();
    } catch (e) { }

    // inject simple CSS for selected table highlight
    try {
        $('<style>.selected-table{box-shadow:0 0 0 3px rgba(255,165,0,0.35) !important; border:2px solid #ffb84d !important;}</style>').appendTo('head');
    } catch (e) { }

    // New Order button: open modal for currently selected table
    $(document).on('click', '#newOrderBtn, .new-order-btn, #openNewOrder', function () {
        if (window.currentSelectedTableNo) {
            // If NewOrder page exposes an opener function, call it; otherwise navigate with table query
            if (typeof window.openNewOrderModal === 'function') {
                window.openNewOrderModal(window.currentSelectedTableNo);
            } else {
                window.location.href = '/Repository/NewOrder?table=' + encodeURIComponent(window.currentSelectedTableNo);
            }
        } else {
            // friendly hint
            alert('Please select a table before creating a new order');
        }
    });

    //// Zomato button click handler
    //$('#zomatoBtn').on('click', function () {
    //    isZomatoActive = !isZomatoActive;
    //    updateZomatoUI();
    //});

    //// Swiggy button click handler
    //$('#swiggyBtn').on('click', function () {
    //    isSwiggyActive = !isSwiggyActive;
    //    updateSwiggyUI();
    //});

    //startAutoRefresh();


    //safeCall("renderOrders");
    //safeCall("updateNewOrdersBadge");
});
function dateKeyFromISO(iso) {
    if (!iso) return "";
    const normalized = iso.includes("T") ? iso : iso.replace(" ", "T");
    const d = new Date(normalized);
    if (isNaN(d)) return "";
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${y}-${m}-${day}`;
}

function displayDateFromKey(key) {
    const d = new Date(key);
    return d.toLocaleDateString("en-IN", {
        day: "2-digit",
        month: "short",
        year: "numeric"
    });
}

function ensureAudioUnlocked() {
    if (audioUnlocked) return;
    try {
        const t = new Audio(ALARM_URL);

        t.preload = "auto";
        const p = t.play();
        if (p && typeof p.then === "function") {
            p.then(() => {
                t.pause();
                t.currentTime = 0;
                audioUnlocked = true;
            }).catch(() => {

            });
        } else {

            t.pause();
            t.currentTime = 0;
            audioUnlocked = true;
        }
    } catch (err) {

    }
}

function showStopBeepButton() {
    if ($('#stopBeepBtn').length) return;
    const stopBtn = `
        <button id="stopBeepBtn" class="btn btn-danger btn-sm position-fixed" 
                style="top: 20px; right: 20px; z-index: 9999;">
            <i class="fas fa-bell-slash"></i> Stop Alarm
        </button>
    `;
    $('body').append(stopBtn);
}

function hideStopBeepButton() {
    $('#stopBeepBtn').remove();
}

function playBeep() {

    if (!audioUnlocked) return;
    debugger;
    if (isBeepPlaying) return;
    debugger;
    try {
        if (!audioBeep) {
            audioBeep = new Audio(ALARM_URL);
            audioBeep.loop = true;
            audioBeep.preload = "auto";
        }

        const playPromise = audioBeep.play();
        if (playPromise !== undefined) {
            playPromise
                .then(() => {
                    isBeepPlaying = true;
                    showStopBeepButton();
                })
                .catch((err) => {
                    console.warn("Audio play blocked:", err);
                    isBeepPlaying = false;
                });
        }
    } catch (error) {
        console.error("Audio play exception:", error);
        isBeepPlaying = false;
    }
}

function stopBeep() {
    if (audioBeep) {
        try {
            audioBeep.pause();
            audioBeep.currentTime = 0;
            audioBeep.loop = false;
        } catch (e) {
            console.error("Error stopping audioBeep:", e);
        }
    }
    isBeepPlaying = false;
}

function startTableBeep(tableNo) {

    if (!audioUnlocked) return;
    if (beepTables[tableNo]) return;

    try {
        const audio = new Audio(ALARM_URL);
        audio.loop = true;
        audio.preload = "auto";
        const playPromise = audio.play();
        if (playPromise && typeof playPromise.then === "function") {
            playPromise
                .then(() => {
                    beepTables[tableNo] = audio;
                    showStopBeepButton();
                })
                .catch((err) => {
                    console.warn(`Table ${tableNo} beep blocked:`, err);
                });
        } else {
            // Fallback
            beepTables[tableNo] = audio;
            showStopBeepButton();
        }
    } catch (err) {
        console.error("Table beep creation error:", err);
    }
}

function stopAllTableBeeps() {
    Object.keys(beepTables).forEach((tableNo) => {
        try {
            if (beepTables[tableNo]) {
                beepTables[tableNo].pause();
                beepTables[tableNo].currentTime = 0;
                beepTables[tableNo].loop = false;
            }
        } catch (error) {
            console.error(`Error stopping beep for table ${tableNo}:`, error);
        } finally {
            delete beepTables[tableNo];
        }
    });
}

function stopAllBeeps() {
    stopBeep();
    stopAllTableBeeps();
    hideStopBeepButton();
}

function isAnyAlarmPlaying() {
    if (isBeepPlaying) return true;

    if (Object.keys(beepTables).length > 0) return true;

    return false;
}

function checkAndStopBeepIfNoProblems() {
    const data = window.liveOrdersData || [];
    const newOrders = data.filter((order) => order.orderStatusId === 1);
    const overdueOrders = data.filter((order) => {
        if (order.orderStatusId !== 2) return false;
        const acceptedTime = orderAcceptedTimes[order.id];
        if (!acceptedTime) return false;
        const elapsed = Date.now() - acceptedTime;
        return elapsed > 10 * 60 * 1000;
    });

    if (newOrders.length === 0 && overdueOrders.length === 0) {
        stopAllBeeps();
    }
}

$(document).on("click keydown touchstart pointerdown", ensureAudioUnlocked);

$(document).on("click", "#stopBeepBtn", function () {
    stopAllBeeps();
});

function initHomeSwitch() {
    $.ajax({
        url: '/Repository/GetAvailabilityHomeDelivery',
        method: 'GET',
        contentType: 'application/json',
        success: function (data) {
            isHomeActive = data;
            updateHomeUI();
        },
        error: function (xhr, status, error) {
        }
    });
}

function setTodayDate() {
    const today = new Date();
    const dateString = today.toISOString().split('T')[0];
    $('#startDate').val(dateString);
    $('#endDate').val(dateString);
}

// Calculate and display revenue
function calculateRevenue(orders) {
    if (!orders || orders.length === 0) {
        $('#totalRevenue').text('₹0.00');
        $('#orderCount').text('0 Orders');
        return;
    }

    // Normalize to one summary per orderId (use `amount` field returned by API)
    const unique = {};
    orders.forEach(o => {
        const id = o.orderId ?? o.order_id ?? o.id ?? (o.OrderId ?? null);
        const amt = Number(o.amount ?? o.Amount ?? o.finalAmount ?? o.FinalAmount ?? o.TotalAmount ?? o.total ?? 0) || 0;
        if (!id) {
            // fallback to createdAt-based unique key
            const key = String(o.createdAt ?? o.Date ?? o.date ?? new Date().toISOString());
            unique[`__${key}_${Math.random().toString(36).slice(2, 7)}`] = { amount: amt, createdAt: o.createdAt ?? o.Date ?? o.date };
        } else if (!unique[id]) {
            unique[id] = { amount: amt, createdAt: o.createdAt ?? o.Date ?? o.date };
        }
    });

    // Group by local date (ignore time) to get day-wise revenue
    const daily = {};
    Object.values(unique).forEach(u => {
        const parsed = Date.parse(u.createdAt);
        if (isNaN(parsed)) return;
        const d = new Date(parsed);
        const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        daily[key] = (daily[key] || 0) + Number(u.amount || 0);
    });

    const totalRevenue = Object.values(daily).reduce((a, b) => a + b, 0);
    const orderCount = Object.keys(unique).length;

    $('#totalRevenue').text(`₹${totalRevenue.toFixed(2)}`);
    $('#orderCount').text(`${orderCount} Orders`);
}

function updateRevenueDateRange(startDate, endDate) {
    const start = new Date(startDate);
    const end = new Date(endDate);

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    start.setHours(0, 0, 0, 0);
    end.setHours(0, 0, 0, 0);

    if (start.getTime() === today.getTime() && end.getTime() === today.getTime()) {
        $('#revenueDateRange').text("Today's Revenue");
    } else if (start.getTime() === end.getTime()) {
        $('#revenueDateRange').text(`Revenue for ${start.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}`);
    } else {
        $('#revenueDateRange').text(`Revenue from ${start.toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })} to ${end.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}`);
    }
}

function applyDateFilter() {
    const start = $('#startDate').val();
    const end = $('#endDate').val();

    if (!start || !end) {
        alert("Please select both start and end dates");
        return;
    }

    // Build an inclusive start (>= startOfDay) and exclusive end (< nextDay) range
    const startOfDay = Date.parse(start + 'T00:00:00'); // local midnight
    const nextOfEndDay = Date.parse(end + 'T00:00:00') + 24 * 60 * 60 * 1000; // exclusive

    if (isNaN(startOfDay) || isNaN(nextOfEndDay) || startOfDay >= nextOfEndDay) {
        alert("Start date cannot be after end date");
        return;
    }

    // Filter the data strictly using createdAt (OrderSummary) when available, falling back to tolerant fields
    filteredOrdersData = allOrdersData.filter(order => {
        const dval = order.createdAt ?? order.createdDate ?? order.CreatedDate ?? order.Date ?? order.date ?? order.completedAt ?? order.CompletedDate;
        if (!dval) return false;
        const t = Date.parse(dval);
        if (isNaN(t)) return false;
        return t >= startOfDay && t < nextOfEndDay;
    });

    // Update revenue display (use filtered data)
    updateRevenueDateRange(start, end);
    calculateRevenue(filteredOrdersData);


    currentPage = 1;
    hasMoreData = true;
    $('#orderHistoryTable tbody').empty();
    $('#noMoreData').removeClass('active');

    displayOrdersPage(filteredOrdersData);
}

function clearDateFilter() {
    setTodayDate();
    applyDateFilter();
}

function updateHomeUI() {

    if (isHomeActive) {
        $('#HomeBtn')
            .removeClass('connect')
            .addClass('disconnect')
            .text('Disconnect Home Delivery Orders');
        $('#HomeStatus')
            .text('Connected')
            .removeClass('disconnected sync-offline')
            .addClass('connected sync-online');
    } else {
        $('#HomeBtn')
            .removeClass('disconnect')
            .addClass('connect')
            .text('Connect Home Delivery Orders');
        $('#HomeStatus')
            .text('Disconnected')
            .removeClass('connected sync-online')
            .addClass('disconnected sync-offline');
    }
}

function toggleHomeAvailability(isAvailable) {

    $.ajax({
        url: '/Repository/SetAvailabilityHomeDelivery',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(isAvailable),
        success: function () { },
        error: function () { }
    });
}

function setupInfiniteScroll() {
    const container = $('#tableContainer');
    container.on('scroll', function () {
        if (isLoading || !hasMoreData) return;
        const scrollTop = container.scrollTop();
        const scrollHeight = container[0].scrollHeight;
        const clientHeight = container.height();
        if (scrollTop + clientHeight >= scrollHeight - 100) {
            loadMoreOrders();
        }
    });
}

function loadOrderHistory(isInitial = false) {
    if (isInitial) {
        currentPage = 1;
        hasMoreData = true;
    }
    isLoading = true;
    $('#loadingSpinner').addClass('active');
    $.ajax({
        url: '/Repository/GetOrderHistory',
        type: 'GET',
        success: function (data) {
            allOrdersData = data || [];
            const start = $('#startDate').val();
            const end = $('#endDate').val();
            if (start && end) {
                const startDate = new Date(start);
                startDate.setHours(0, 0, 0, 0);
                const endDate = new Date(end);
                endDate.setHours(23, 59, 59, 999);
                filteredOrdersData = allOrdersData.filter(order => {
                    const dval = order.date ?? order.Date ?? order.SummaryDate ?? order.completedAt ?? order.CompletedDate ?? order.createdDate ?? order.CreatedDate;
                    if (!dval) return false;
                    const orderDate = new Date(dval);
                    if (isNaN(orderDate)) return false;
                    return orderDate >= startDate && orderDate <= endDate;
                });
            } else {
                filteredOrdersData = allOrdersData;
            }

            if (isInitial) {
                $('#orderHistoryTable tbody').empty();
                updateRevenueDateRange(start, end);
            }

            // Calculate revenue from filtered (summary) rows
            calculateRevenue(filteredOrdersData);
            displayOrdersPage(filteredOrdersData);

            isLoading = false;
            $('#loadingSpinner').removeClass('active');
        },
        error: function (xhr, status, error) {
            console.error('Error loading order history:', error);
            $('#orderHistoryTable tbody').html(`
                    <tr><td colspan="10" class="text-center text-danger">Failed to load order history</td></tr>
                `);
            isLoading = false;
            $('#loadingSpinner').removeClass('active');
            hasMoreData = false;
        }
    });
}

function loadMoreOrders() {
    if (isLoading || !hasMoreData) return;
    currentPage++;
    displayOrdersPage(filteredOrdersData);
}

function displayOrdersPage(dataToDisplay = filteredOrdersData) {
    // Build grouped & paged rows as before
    if (!dataToDisplay || dataToDisplay.length === 0) {
        if (currentPage === 1) {
            $('#orderHistoryTable tbody').html(`
    <tr><td colspan="10" class="text-center text-muted">No orders found for the selected date range.</td></tr>
                    `);
        }
        hasMoreData = false;
        $('#noMoreData').addClass('active');
        if (typeof reinitOrderHistoryDataTable === 'function') reinitOrderHistoryDataTable();
        return;
    }

    const groupedOrders = {};
    dataToDisplay.forEach(order => {
        if (!groupedOrders[order.orderId]) groupedOrders[order.orderId] = [];
        groupedOrders[order.orderId].push(order);
    });

    const sortedOrderIds = Object.keys(groupedOrders).sort((a, b) => {
        const na = a.match(/\\d+/);
        const nb = b.match(/\\d+/);
        if (na && nb) return Number(nb[0]) - Number(na[0]);
        return b.localeCompare(a);
    });

    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const pageOrderIds = sortedOrderIds.slice(startIndex, endIndex);

    if (pageOrderIds.length === 0) {
        hasMoreData = false;
        $('#noMoreData').addClass('active');
        if (typeof reinitOrderHistoryDataTable === 'function') reinitOrderHistoryDataTable();
        return;
    }

    const rowsData = pageOrderIds.map(orderId => {
        const orders = groupedOrders[orderId];
        const first = orders[0];
        const totalItems = orders.length;
        const created = first.createdAt ?? first.date ?? first.Date ?? first.completedAt ?? '';
        const date = created ? new Date(created).toLocaleString('en-IN') : '';
        const amount = Number(first.amount ?? first.finalAmount ?? first.FinalAmount ?? first.TotalAmount ?? first.total ?? 0) || 0;
        return [
            `<strong>${first.orderId}</strong>`,
            first.customerName || 'N/A',
            first.phone || 'N/A',
            first.tableNo ? 'Table ' + first.tableNo : 'N/A',
            `<div style="max-width:200px; white-space:normal;">${first.itemName || 'N/A'}</div>`,
            `<strong>₹${amount.toFixed(2)}</strong>`,
            totalItems,
            first.paymentMode || 'Online',
            date,
            `<button class="btn btn-sm btn-info btn-view-bill" data-order-id="${first.orderId}">View Bill</button>`
        ];
    });

    try {
        if (typeof $.fn.DataTable === 'undefined') {
            const tbody = $('#orderHistoryTable tbody');
            if (currentPage === 1) tbody.empty();
            rowsData.forEach(r => {
                const rowHtml = '<tr class="table-success">' + r.map(c => `<td>${c}</td>`).join('') + '</tr>';
                tbody.append(rowHtml);
            });
            if (endIndex >= sortedOrderIds.length) {
                hasMoreData = false;
                $('#noMoreData').addClass('active');
            }
            return;
        }

        const $table = $('#orderHistoryTable');

        if (!$.fn.DataTable.isDataTable('#orderHistoryTable')) {
            $table.DataTable({
                order: [[0, 'desc']],
                responsive: { details: { type: 'column', target: -1 } },
                paging: false,
                searching: false,
                info: false,
                ordering: true,
                autoWidth: false,
                deferRender: true,
                columnDefs: [{ orderable: false, targets: -1 }, { className: 'dt-control', targets: -1 }],
                destroy: true, // allow re-init safely
                initComplete: function () {
                    try { this.columns.adjust(); if (this.responsive) this.responsive.recalc(); } catch (e) { }
                }
            });
        }

        const table = $table.DataTable();

        if (currentPage === 1) table.clear();

        table.rows.add(rowsData);
        try { table.order([[0, 'desc']]); } catch (e) { }
        table.draw(false);
        try { table.columns.adjust(); if (table.responsive) table.responsive.recalc(); } catch (e) { }

        if (endIndex >= sortedOrderIds.length) {
            hasMoreData = false;
            $('#noMoreData').addClass('active');
        }
    } catch (err) {
        console.warn('displayOrdersPage DataTable error:', err);
        const tbody = $('#orderHistoryTable tbody');
        if (currentPage === 1) tbody.empty();
        rowsData.forEach(r => {
            const rowHtml = '<tr class="table-success">' + r.map(c => `<td>${c}</td>`).join('') + '</tr>';
            tbody.append(rowHtml);
        });
    }
}

$(document).on('click', '.btn-view-order', function () {
    const encoded = $(this).attr('data-orders');
    if (!encoded) {
        alert('No order details available');
        return;
    }
    try {
        const orders = JSON.parse(decodeURIComponent(encoded));
        viewOrderDetailss(orders);
    } catch (err) {
        console.error('Failed to parse order details:', err);
        alert('Failed to load order details');
    }
});

function viewOrderDetailss(orders) {
    if (!orders || orders.length === 0) {
        alert('No order details available');
        return;
    }

    const firstOrder = orders[0];
    // tolerant field mapping
    const orderIdVal = firstOrder.orderId || firstOrder.OrderId || firstOrder.order_id || firstOrder.id || '';
    const customerVal = firstOrder.customerName || firstOrder.customer || firstOrder.name || '';
    const phoneVal = firstOrder.phone || firstOrder.customerPhone || firstOrder.phoneNumber || '';
    const addrVal = firstOrder.address || firstOrder.addr || '';
    const totalAmount = orders.reduce((sum, order) => sum + (Number(order.price) || 0), 0);
    const dateValRaw = firstOrder.date || firstOrder.createdDate || firstOrder.createdAt || firstOrder.timestamp || new Date();
    const date = new Date(dateValRaw).toLocaleString('en-IN', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });

    let itemsHtml = '';
    orders.forEach((order, index) => {
        const halfQty = Number(order.halfPortion) || 0;
        const fullQty = Number(order.fullPortion) || 0;
        const qtyText = halfQty > 0 && fullQty > 0
            ? `${halfQty} Half + ${fullQty} Full`
            : halfQty > 0
                ? `${halfQty} Half`
                : `${fullQty} Full`;

        itemsHtml += `
                <div class="bill-item">
                    <span><strong>${index + 1}.</strong> ${order.itemName || 'N/A'} <small>(${qtyText})</small></span>
                    <span><strong>₹${(Number(order.price) || 0).toFixed(2)}</strong></span>
                </div>
            `;
    });

    const subtotal = totalAmount;
    const discount = Number(firstOrder.discount) || 0;
    const finalTotal = subtotal - discount;
    const billHtml = `
    <div class="bill" id="printable-bill" style="font-family: Arial; width:260px;">

        <!-- Header -->
        <div style="text-align:center; border-bottom:1px dashed #000; padding-bottom:6px;">
            <div style="font-size:18px; font-weight:bold;">Grill N Shakes</div>
            <div style="font-size:11px;">123 Restaurant Street, Ulwe</div>
            <div style="font-size:11px;">📞 +918928484618</div>
        </div>

        <!-- Order Details -->
        <div style="margin-top:6px; font-size:12px;">
                <div><strong>Order:</strong> #${orderIdVal}</div>
            <div><strong>Date:</strong> ${date}</div>
                <div><strong>Name:</strong> ${customerVal || 'Walk-in'}</div>
                <div><strong>Phone:</strong> ${phoneVal || 'N/A'}</div>
            <div><strong>Table:</strong> ${firstOrder.tableNo || 'N/A'}</div>
            <div><strong>Pay:</strong> ${firstOrder.paymentMode || 'CASH'}</div>
        </div>

        <!-- Items -->
        <div style="border-top:1px dashed #000; margin-top:6px; padding-top:6px;">
            <strong style="font-size:13px;">Items (${orders.length})</strong>
            <div style="margin-top:4px; font-size:12px;">
                ${itemsHtml}
            </div>
        </div>

        <!-- Totals -->
        <div style="border-top:1px dashed #000; margin-top:6px; padding-top:6px; font-size:13px;">
            <div style="display:flex; justify-content:space-between;">
                <span>Subtotal</span>
                <span>₹${subtotal.toFixed(2)}</span>
            </div>

            <div style="display:flex; justify-content:space-between; font-weight:bold; font-size:14px; margin-top:4px;">
                <span>Total</span>
                <span>₹${totalAmount.toFixed(2)}</span>
            </div>
        </div>

        <!-- Footer -->
        <div style="text-align:center; margin-top:8px; font-size:11px; border-top:1px dashed #000; padding-top:6px;">
            <div>🙏 Thank you!</div>
            <div>GST: 27XXXXX1234X1ZX</div>
        </div>

    </div>
    `;

    $('#bill-content').html(billHtml);

    // Prepare a canonical object for thermal printing and store globally so history page can use it
    try {
        const thermalItems = billData.map(item => {
            const qty = (Number(item.fullPortion) || 0) + (Number(item.halfPortion) || 0) || Number(item.quantity) || 1;
            const price = Number(item.fullPrice) || Number(item.price) || 0;
            return { name: item.itemName || item.name || '-', quantity: qty, price: price };
        });

        const thermalOrder = {
            orderId: firstOrder.orderId || firstOrder.OrderId || '',
            customer: firstOrder.customerName || firstOrder.customer || '',
            phone: firstOrder.phone || '',
            address: firstOrder.address || firstOrder.customerAddress || '',
            timestamp: firstOrder.createdDate || firstOrder.createdAt || firstOrder.date || new Date(),
            items: thermalItems,
            total: finalTotal,
            discountAmount: discount
        };

        // Expose for history.js or other pages to trigger thermal print
        window.currentBillData = thermalOrder;
    } catch (e) {
        console.warn('Failed to prepare thermal payload', e);
    }

    $('#bill-modal').addClass('active');
    $('#bill-modal').fadeIn(300);

}

function closeBillModal() {
    $('#bill-modal').fadeOut(300);
}

function printBill() {
    window.print();
}

function downloadBill() {
    if (typeof window.downloadBill === 'function') {
        window.downloadBill();
    } else {
        alert('PDF download functionality not available');
    }
}

$(document).on('click', '#bill-modal', function (e) {
    if (e.target.id === 'bill-modal') {
        closeBillModal();
    }
});
function buildBillUI(billData) {

    // Normalize input: support either an array of item rows or an object with items[]
    if (!billData) {
        alert("No bill found.");
        return;
    }

    let itemsArray = [];
    let orderRoot = {};
    if (Array.isArray(billData)) {
        itemsArray = billData;
        orderRoot = billData[0] || {};
    } else if (billData.items && Array.isArray(billData.items)) {
        itemsArray = billData.items;
        orderRoot = billData;
    } else if (typeof billData === 'object') {
        // single-line record
        itemsArray = [billData];
        orderRoot = billData;
    }

    if (itemsArray.length === 0) {
        alert('No bill items found.');
        return;
    }

    // tolerant field mapping
    const orderIdVal = orderRoot.orderId || orderRoot.OrderId || orderRoot.order_id || orderRoot.id || '';
    let customerVal = orderRoot.customerName || orderRoot.customer || orderRoot.name || '';
    let phoneVal = orderRoot.phone || orderRoot.customerPhone || orderRoot.phoneNumber || '';
    let addrVal = orderRoot.address || orderRoot.addr || '';
    const dateValRaw = orderRoot.createdDate || orderRoot.createdAt || orderRoot.date || orderRoot.timestamp || new Date();

    const subtotal = itemsArray.reduce((sum, item) => {
        const fullQty = Number(item.fullPortion) || 0;
        const halfQty = Number(item.halfPortion) || 0;
        const qty = (fullQty + halfQty) || Number(item.quantity) || 1;
        const unitPrice = Number(item.fullPrice) || Number(item.price) || 0;
        return sum + (qty * unitPrice);
    }, 0);

    const discount = Number(orderRoot.discountAmount || orderRoot.discount || 0);
    const finalTotal = subtotal - discount;

    let itemsHtml = '';
    itemsArray.forEach((item, i) => {
        const fullQty = Number(item.fullPortion) || 0;
        const halfQty = Number(item.halfPortion) || 0;
        const qtyText = fullQty || halfQty ? `${fullQty}F ${halfQty}H` : (item.quantity || 1);
        const unitPrice = Number(item.fullPrice) || Number(item.price) || 0;
        const qty = (fullQty + halfQty) || Number(item.quantity) || 1;
        const totalItemPrice = qty * unitPrice;

        itemsHtml += `
            <div style="display:flex; font-size:14px; justify-content:space-between;">
                <span>${i + 1}. ${item.itemName || item.name || '-'} (${qtyText})</span>
                <span>₹${totalItemPrice.toFixed(2)}</span>
            </div>
            `;
    });

    const date = new Date(dateValRaw).toLocaleString('en-IN', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });

    const billHtml = `
    <div id="printable-bill" style="
        width:220px;
        font-family:'Courier New', monospace;
        margin: 0 auto;
        font-size: 14px;
        line-height: 1.2;
        padding:4px;
        font-weight: 700;
    ">

        <div style="text-align:center;">
            <strong>GRILL N SHAKES</strong><br/>
            Plot C-179 Sector-19<br/>
            Ulwe, Navi Mumbai<br/>
            +91 8928484618
            <hr style="border-top:1px dashed #000;">
        </div>

        <div style="font-size:14px;">
            Order ID : <strong>#${orderIdVal}</strong><br/>
            Date     : ${date}<br/>
            Name     : ${customerVal}<br/>
            Phone    : ${phoneVal}<br/>
            Notes: ${orderRoot.specialInstructions || orderRoot.notes || ''}<br/>
        </div>

        <hr style="border-top:1px dashed #000;">

        <strong>Items</strong><br/>

        ${itemsHtml}

        <hr style="border-top:1px dashed #000;">

        <div style="display:flex; font-size:14px; justify-content:space-between;">
            <span>Subtotal:</span>
            <span>₹${subtotal.toFixed(2)}</span>
        </div>

        <div style="display:flex; font-size:14px; justify-content:space-between;">
            <span>Discount:</span>
            <span>₹${discount.toFixed(2)}</span>
        </div>

        <hr style="border-top:1px dashed #000;">

        <div style="display:flex; font-size:15px; justify-content:space-between; font-weight:900;">
            <span>Total:</span>
            <span>₹${finalTotal.toFixed(2)}</span>
        </div>

        <br/>

        <div style="text-align:center; font-size:15px;">
            Thank you for visiting!<br/><br/>
        </div>

        <div style="height:40px;"></div>

    </div>
    `;

    $("#bill-content").html(billHtml);

    // Fallback: if order-level fields are empty, try to extract from items
    try {
        if (!customerVal || !phoneVal || !addrVal) {
            for (const it of itemsArray) {
                if (!customerVal) customerVal = customerVal || it.customerName || it.customer || it.orderCustomer || '';
                if (!phoneVal) phoneVal = phoneVal || it.phone || it.customerPhone || '';
                if (!addrVal) addrVal = addrVal || it.address || it.addr || '';
                if (customerVal && phoneVal && addrVal) break;
            }
        }
    } catch (e) {
        console.warn('fallback extraction failed', e);
    }

    // Prepare global canonical payload for thermal printing
    try {
        const thermalItems = itemsArray.map(item => ({ name: item.itemName || item.name || '-', quantity: (Number(item.fullPortion) || 0) + (Number(item.halfPortion) || 0) || Number(item.quantity) || 1, price: Number(item.fullPrice || item.price || 0) }));
        window.currentBillData = {
            orderId: orderIdVal,
            customer: customerVal || '',
            phone: phoneVal || '',
            address: addrVal || '',
            timestamp: dateValRaw,
            items: thermalItems,
            total: finalTotal,
            discountAmount: discount
        };
        // debug log to help verify payload used for thermal printing
    } catch (e) {
        console.warn('prepare currentBillData failed', e);
        window.currentBillData = null;
    }

    $("#bill-modal").fadeIn(300);
}


$(document).on('click', '.btn-view-bill', function () {
    const orderId = $(this).attr('data-order-id');


    $.ajax({
        url: `/Repository/GetBillData?orderId=${orderId}`,
        method: "GET",
        success: function (billResponse) {
            buildBillUI(billResponse);
        },
        error: function () {
            alert("Failed to load bill.");
        }
    });
});

$(document).keyup(function (e) {
    if (e.key === "Escape") {
        closeBillModal();
    }
});

$('#HomeBtn').on('click', function () {
    isHomeActive = !isHomeActive;
    updateHomeUI();
    toggleHomeAvailability(isHomeActive);
});
$('#btnFilter').on('click', function () {
    applyDateFilter();
});
$('#btnClear').on('click', function () {
    clearDateFilter();
});

$(document).on("click", ".card", function () {
    const tableTitle = $(this).find(".card-title").text();
    $(".modal-title").text(tableTitle);
    currentTableNo = parseInt(tableTitle.replace("Table", ""));
    $(".card").addClass("m-3 card-click-animation card-click-opacity");
    $(this).removeClass("m-3 card-click-opacity");
    updateOrderDetails(tableTitle);
});

// Modal button: Accept or Complete (opens payment modal)
$(document).off("click", "#confirmOrderBtn").on("click", "#confirmOrderBtn", function () {
    const $btn = $(this);
    const tableTitle = $(".modal-title").text();
    const tableNo = parseInt(tableTitle.replace("Table", ""));
    const data = window.liveOrdersData || [];

    // Accept assigning orders
    if ($btn.data("mode") !== "complete") {
        const assigningOrders = data.filter(
            (order) => order.tableNo === tableNo && order.orderStatusId === 1
        );
        if (assigningOrders.length === 0) {
            safeCall("showSuccessMessage", "No assigning orders to accept!");
            return;
        }

        let completedAccepts = 0;
        const totalAccepts = assigningOrders.length;

        assigningOrders.forEach((order) => {
            acceptOrder(order, function () {
                completedAccepts++;
                if (completedAccepts === totalAccepts) {
                    safeCall("showSuccessMessage", `Accepted ${totalAccepts} order(s) for Table ${tableNo}!`);

                    //setTimeout(() => {
                    //    checkAndStopBeepIfNoProblems();
                    //}, 100);

                    //setTimeout(() => {
                    updateConfirmOrderBtn(tableNo);
                    updateOrderDetails(tableTitle);
                    //}, 500);
                }
            });
        });

        return;
    }

    // Complete flow -> open payment modal for active orders
    const activeOrders = data.filter((order) => order.tableNo === tableNo && order.orderStatusId === 2);
    if (activeOrders.length === 0) {
        safeCall("showSuccessMessage", "No active orders to complete!");
        return;
    }

    // Keep selections (ids) and context
    currentOrderData = activeOrders.map((o) => o.id);
    paymentContext = "table";

    // Reset payment modal UI
    $(".payment-option").removeClass("selected");
    $("#confirmPayment").prop("disabled", true);
    $("#paymentError").removeClass("active");
    $("#discountInput").val('');

    // Compute subtotal now and bind summary values so modal shows them immediately
    window.currentModalOrderTotal = computeCurrentSubtotal() || 0;
    const subtotal = Number(window.currentModalOrderTotal) || 0;
    const discountAmt = 0;
    const finalAmount = Math.max(0, subtotal - discountAmt);

    $('#paymentSubtotal').text(`₹${subtotal.toFixed(2)}`);
    $('#paymentDiscount').text(`₹${discountAmt.toFixed(2)}`);
    $('#paymentFinal').text(`₹${finalAmount.toFixed(2)}`);

    // Open payment modal
    // allow the in-progress modal to hide first if present
    $("#divInProgressModal").modal("hide");
    $("#divInProgressModalHome").modal("hide");
    //setTimeout(() => {
    $("#paymentModal").addClass("active");
    const d = document.getElementById("discountInput");
    if (d) d.focus();
    //}, 250);
});

// When in-progress modal shows, set button mode
$(document).on("show.bs.modal", "#divInProgressModal", function () {
    const tableTitle = $(this).find(".modal-title").text().trim();
    const tableNo = parseInt(tableTitle.replace("Table", ""));
    updateConfirmOrderBtn(tableNo);
});

// ========== Data Loading ==========
function loadTableOrders() {
    $.ajax({
        url: "/Repository/GetOrder",
        type: "GET",
        success: function (data) {

            window.liveOrdersData = data || [];
            filterCompletedOrdersToHistory();

            const now = Date.now();
            const assigningOrders = window.liveOrdersData.filter((o) => o.orderStatusId === 1);
            const activeOrders = window.liveOrdersData.filter((o) => o.orderStatusId === 2);

            // Track received times for assigning
            assigningOrders.forEach((order) => {
                if (!orderReceivedTimes[order.id]) {
                    orderReceivedTimes[order.id] = order.date ? new Date(order.date).getTime() : now;
                }
            });

            // Track accepted times + start completion timers
            activeOrders.forEach((order) => {
                if (!orderAcceptedTimes[order.id]) {
                    orderAcceptedTimes[order.id] = now;
                }

                if (!completionTimers[order.id]) {
                    const acceptedTime = orderAcceptedTimes[order.id];
                    const elapsed = now - acceptedTime;
                    const msLeft = Math.max(0, 10 * 60 * 1000 - elapsed);

                    completionTimers[order.id] = setTimeout(function completionReminder() {
                        const stillActive = (window.liveOrdersData || []).some(
                            (o) => o.id === order.id && o.orderStatusId === 2
                        );
                        if (stillActive) {
                            // Start per-table beep for overdue orders
                            startTableBeep(order.tableNo);
                            // universal beep as well
                            playBeep();
                            // Next reminder in 5 minutes
                            completionTimers[order.id] = setTimeout(
                                completionReminder,
                                30 * 60 * 1000
                            );
                        } else {
                            clearTimeout(completionTimers[order.id]);
                            delete completionTimers[order.id];
                            delete orderAcceptedTimes[order.id];
                        }
                    }, msLeft);
                }
            });

            Object.keys(completionTimers).forEach((orderId) => {
                if (!activeOrders.some((o) => o.id == orderId)) {
                    clearTimeout(completionTimers[orderId]);
                    delete completionTimers[orderId];
                    delete orderAcceptedTimes[orderId];
                }
            });

            Object.keys(orderReceivedTimes).forEach((orderId) => {
                if (!assigningOrders.some((o) => o.id == orderId)) {
                    delete orderReceivedTimes[orderId];
                    if (reminderTimers[orderId]) {
                        clearTimeout(reminderTimers[orderId]);
                        delete reminderTimers[orderId];
                    }
                }
            });

            const newOrderTables = [...new Set(assigningOrders.map((o) => o.tableNo))];

            // Trigger universal beep if there are any new orders
            if (newOrderTables.length > 0 && !isBeepPlaying) {
                playBeep();
            }

            // Stop beeps if neither new nor overdue orders exist
            if (newOrderTables.length === 0) {
                checkAndStopBeepIfNoProblems();
            }

            // Stop per-table beeps for tables without new/overdue orders
            Object.keys(beepTables).forEach((tableNo) => {
                const hasNewOrders = newOrderTables.includes(Number(tableNo));
                const hasOverdueOrders = activeOrders.some((order) => {
                    if (order.tableNo !== Number(tableNo)) return false;
                    const acceptedTime = orderAcceptedTimes[order.id];
                    if (!acceptedTime) return false;
                    const elapsed = now - acceptedTime;
                    return elapsed > 30 * 60 * 1000;
                });

                if (!hasNewOrders && !hasOverdueOrders) {
                    try {
                        beepTables[tableNo].pause();
                        beepTables[tableNo].currentTime = 0;
                        beepTables[tableNo].loop = false;
                    } catch (e) {
                        console.warn(`Error stopping table beep ${tableNo}:`, e);
                    }
                    delete beepTables[tableNo];
                }
            });

            // 5-minute reminder logic for assigning orders (per item)
            assigningOrders.forEach((order) => {
                if (!reminderTimers[order.id]) {
                    const receivedTime = orderReceivedTimes[order.id];
                    const elapsed = now - receivedTime;
                    const msLeft = Math.max(0, 5 * 60 * 1000 - elapsed);

                    reminderTimers[order.id] = setTimeout(function reminder() {
                        const stillAssigning = (window.liveOrdersData || []).some(
                            (o) => o.id === order.id && o.orderStatusId === 1
                        );
                        if (stillAssigning) {
                            // Per-table beep and universal beep
                            startTableBeep(order.tableNo);
                            playBeep();
                            // Next reminder in 30 minutes
                            reminderTimers[order.id] = setTimeout(reminder, 30 * 60 * 1000);
                        } else {
                            clearTimeout(reminderTimers[order.id]);
                            delete reminderTimers[order.id];
                            delete orderReceivedTimes[order.id];
                        }
                    }, msLeft);
                }
            });

            // Auto-open the modal for brand-new tables with assigning orders
            if (!window.prevNewOrderTables) window.prevNewOrderTables = [];
            newOrderTables.forEach((tableNo) => {
                if (!window.prevNewOrderTables.includes(tableNo)) {
                    $(".modal-title").text("Table " + tableNo);
                    $("#divInProgressModal").modal("show");
                    updateOrderDetails("Table " + tableNo);
                }
            });
            window.prevNewOrderTables = newOrderTables;

            initializeLiveOrdersTable(window.liveOrdersData);
            bindDynamicTable();
            updateStats();
        },
        error: function () {
            window.liveOrdersData = [];
            initializeLiveOrdersTable(window.liveOrdersData);
            bindDynamicTable();
            updateStats();
        }
    });
}

function filterCompletedOrdersToHistory() {
    if (!window.liveOrdersData) return;
    const completedOrders = window.liveOrdersData.filter((order) => order.orderStatusId === 3);
    const activeOrders = window.liveOrdersData.filter((order) => order.orderStatusId !== 3);

    completedOrders.forEach((order) => {
        if (!orderHistory.find((h) => h.id === order.id)) {
            orderHistory.push({
                ...order,
                completedAt: order.completedAt || new Date().toISOString()
            });
        }
    });

    window.liveOrdersData = activeOrders;
}

function loadTableCount() {
    $.ajax({
        url: "/Repository/GetTableCount",
        type: "GET",
        success: function (data) {
            let count = typeof data === "object" ? data.count : parseInt(data);
            arrTable = Array.from({ length: count }, (_, i) => i + 1);
            bindDynamicTable();
        },
        error: function () {
            safeCall("showSuccessMessage", "Failed to load table count!");
        }
    });
}

// ========== DataTable (live) ==========

function initializeLiveOrdersTable(data) {
    if ($.fn.DataTable.isDataTable("#liveOrdersTable")) {
        liveOrdersTable.clear().rows.add(data).draw();
        return;
    }

    liveOrdersTable = $("#liveOrdersTable").DataTable({
        data: data,
        pageLength: 10,
        order: [[4, "desc"]],
        columns: [
            { data: "tableNo", title: "Table No" },
            { data: "id", title: "ID" },
            { data: "itemName", title: "Item Name" },
            { data: "halfPortion", title: "Half" },
            { data: "fullPortion", title: "Full" },
            { data: "price", title: "Price" },
            { data: "orderStatusId", title: "Status ID" },
            { data: "date", title: "Date" }
        ],
        rowCallback: function (row, rowData) {
            if (rowData.orderStatusId === 1) {
                $(row).addClass("bg-warning");
            } else if (rowData.orderStatusId === 2) {
                const acceptedTime = orderAcceptedTimes[rowData.id];
                if (acceptedTime && Date.now() - acceptedTime > 20 * 60 * 1000) {
                    $(row).addClass("bg-danger text-white"); // Overdue orders in red
                } else {
                    $(row).addClass("bg-info text-white");
                }
            }
        },
        autoWidth: false,
        destroy: true
    });
}

// ========== Cards (tables) ==========

function bindDynamicTable() {

    let tblhtml = "";
    const data = window.liveOrdersData || [];
    if (!Array.isArray(arrTable)) return;

    arrTable.forEach(function (tbl, i) {
        if (i % 3 == 0)
            tblhtml +=
                '<div class="row p-2"><div class="card-group" style="width: 100%; height: 100%;">';

        const tableOrders = data.filter(
            (order) => order.tableNo === tbl && order.orderStatusId !== 3
        );
        let totalPrice = 0;
        tableOrders.forEach((order) => {
            totalPrice +=
                (Number(order.halfPortion) + Number(order.fullPortion)) *
                Number(order.price);
        });


        let cardClass = "bg-success";
        let statusText = "Available";

        if (tableOrders.length > 0) {

            const hasAssigningOrders = tableOrders.some((o) => o.orderStatusId === 1);
            const hasActiveOrders = tableOrders.some((o) => o.orderStatusId === 2);

            const hasOverdueOrders = tableOrders.some((order) => {
                if (order.orderStatusId !== 2) return false;
                const acceptedTime = orderAcceptedTimes[order.id];
                if (!acceptedTime) return false;
                return Date.now() - acceptedTime > 10 * 60 * 1000;
            });

            if (hasAssigningOrders) {
                cardClass = "bg-warning";
                statusText = "Assigning Order";
            } else if (hasOverdueOrders) {
                cardClass = "bg-danger";
                statusText = "Delay in Serving";
            } else if (hasActiveOrders) {
                cardClass = "bg-info";
                statusText = "Active";
            }
        }

        tblhtml += `<div class="card text-white ${cardClass} m-3 card-click-animation" data-toggle="modal" data-target="#divInProgressModal" data-table="${tbl}">`;
        tblhtml += `<div class="card-body table-card-body"><center><h5 class="card-title">Table ${tbl}</h5>`;
        tblhtml += `<div><strong>Total: ₹${totalPrice}</strong></div></center></div>`;
        tblhtml += `<div class="card-footer"><center><small>${statusText}</small></center></div>`;

        // highlight selected table
        try {
            // nothing here, selection handled after DOM insert
        } catch (e) { }
        tblhtml += `</div>`;

        if (i % 3 == 2) tblhtml += "</div></div>";
    });

    if (arrTable.length % 3 !== 0) {
        tblhtml += "</div></div>";
    }

    $("#divTable").html(tblhtml);
    renderOrderHistory();

    // After rendering, attach click handler to select a table
    $(document).off('click', '.card[data-table]').on('click', '.card[data-table]', function (e) {
        var t = $(this).attr('data-table');
        if (!t) return;
        window.currentSelectedTableNo = parseInt(t);

        // Highlight selection
        $('.card[data-table]').removeClass('selected-table');
        $(this).addClass('selected-table');

        // show new order button
        $('[id=newOrderBtn], .new-order-btn, #openNewOrder').show();

        // open modal or update details
        $('.modal-title').text('Table ' + t);
        $('#divInProgressModal').modal('show');
        updateOrderDetails('Table ' + t);
        updateConfirmOrderBtn(parseInt(t));
    });

    // Ensure New Order button inside modal opens NewOrder page with selected table
    $(document).off('click', '#openNewOrderForTable').on('click', '#openNewOrderForTable', function () {
        const t = window.currentSelectedTableNo;
        if (!t) { alert('No table selected'); return; }
        // Open NewOrder route with table query and ensure NewOrder uses the table param
        const url = '/Home/NewOrder?table=' + encodeURIComponent(t);
        // Open in same window (modal will remain open until new page loads)
        window.location.href = url;
    });
}

// ========== History ==========

function renderOrderHistory() {
    if (orderHistory.length === 0) return;

    const getOrderDate = (o) => o.completedAt || o.date;

    const uniqueTables = [...new Set(orderHistory.map((o) => o.tableNo))].sort(
        (a, b) => a - b
    );

    const uniqueDateKeys = [
        ...new Set(orderHistory.map((o) => dateKeyFromISO(getOrderDate(o))))
    ]
        .filter((k) => k !== "")
        .sort((a, b) => b.localeCompare(a));

    let filtered = orderHistory.filter((o) => getOrderDate(o));
    if (selectedHistoryDate) {
        filtered = filtered.filter(
            (o) => dateKeyFromISO(getOrderDate(o)) === selectedHistoryDate
        );
    }
    if (selectedHistoryTableNo) {
        filtered = filtered.filter((o) => o.tableNo == selectedHistoryTableNo);
    }

    const sortedHistory = filtered.sort((a, b) => {
        const da = new Date(getOrderDate(a));
        const db = new Date(getOrderDate(b));
        return db - da;
    });

    let totalHistoryAmount = 0;
    let rowsHtml = "";

    if (sortedHistory.length === 0) {
        rowsHtml = `
            <tr>
                <td colspan="6" class="text-center text-muted">
                    No completed orders${selectedHistoryTableNo ? " for Table " + selectedHistoryTableNo : ""
            }${selectedHistoryDate ? " on " + displayDateFromKey(selectedHistoryDate) : ""}.
                </td>
            </tr>
        `;
    } else {
        sortedHistory.forEach((order) => {
            const qty = Number(order.halfPortion) + Number(order.fullPortion);
            const totalPrice = qty * Number(order.price);
            totalHistoryAmount += totalPrice;

            const displayDate = getOrderDate(order)
                ? new Date(getOrderDate(order)).toLocaleString("en-IN", {
                    year: "numeric",
                    month: "2-digit",
                    day: "2-digit",
                    hour: "2-digit",
                    minute: "2-digit",
                    second: "2-digit",
                    hour12: false
                })
                : "N/A";

            rowsHtml += `
                <tr class="table-success fade-in-row">
                    <td>${order.customerName || "-"}</td>
                    <td><span class="badge badge-secondary">Table ${order.tableNo}</span></td>
                    <td>${order.itemName}</td>
                    <td><span class="badge badge-info">${qty}</span></td>
                    <td><strong>₹${totalPrice}</strong></td>
                    <td><small class="text-success"><i class="fas fa-check-circle"></i> ${displayDate}</small></td>
                </tr>
            `;
        });
    }

    if ($("#orderHistorySection").length !== 0) {
        $("#historyTableBody").html(rowsHtml);
        $(".card-footer strong:first").text(
            `Total Completed Orders: ${sortedHistory.length}`
        );
        $(".card-footer strong:last").text(`Total Revenue: ₹${totalHistoryAmount}`);
    }

    const tableSelect = $("#historyTableFilter");
    const dateSelect = $("#historyDateFilter");
    const currentTables = tableSelect.find("option").length - 1;
    const currentDates = dateSelect.find("option").length - 1;

    if (uniqueTables.length !== currentTables) {
        tableSelect.html(
            `<option value="">All Tables</option>` +
            uniqueTables
                .map(
                    (t) =>
                        `<option value="${t}" ${selectedHistoryTableNo == t ? "selected" : ""
                        }>Table ${t}</option>`
                )
                .join("")
        );
    }

    if (uniqueDateKeys.length !== currentDates) {
        dateSelect.html(
            `<option value="">All Dates</option>` +
            uniqueDateKeys
                .map(
                    (k) =>
                        `<option value="${k}" ${selectedHistoryDate === k ? "selected" : ""
                        }>${displayDateFromKey(k)}</option>`
                )
                .join("")
        );
    }

    // Rebind filters
    $(document)
        .off("change", "#historyTableFilter")
        .on("change", "#historyTableFilter", function () {
            selectedHistoryTableNo = $(this).val();
            renderOrderHistory();
        });
    $(document)
        .off("change", "#historyDateFilter")
        .on("change", "#historyDateFilter", function () {
            selectedHistoryDate = $(this).val();
            renderOrderHistory();
        });
}


function updateOrderDetails(tableTitle) {
    const data = window.liveOrdersData || [];
    const tableNo = parseInt(tableTitle.replace("Table", ""));
    let tableOrders = data.filter(
        (order) => order.tableNo === tableNo && order.orderStatusId !== 3
    );

    // Pagination variables
    let currentPage = 1;
    const rowsPerPage = 10;

    function renderTable(filteredOrders) {
        let detailsHtml = `
            <table class="table table-bordered table-striped" id="orderDetailsTable">
                <thead>
                    <tr>
                        <th  style="display:none">Id</th>
                        <th>Item Name</th>
                        <th>Half</th>
                        <th>Full</th>
                        <th>Total</th>
                        <th>Status</th>
                        <th>Order Time</th>
                        <th>Special Instruction</th>
                        <th style="display:none">Update Order</th>
                        <th>Manage Order</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
        `;

        const totalRows = filteredOrders.length;
        const totalPages = Math.ceil(totalRows / rowsPerPage);
        const startIdx = (currentPage - 1) * rowsPerPage;
        const endIdx = Math.min(startIdx + rowsPerPage, totalRows);

        if (totalRows === 0) {
            detailsHtml += `<tr><td colspan="10" class="text-center text-muted">No active orders for Table ${tableNo}.</td></tr>`;
        } else {
            for (let i = startIdx; i < endIdx; i++) {
                const order = filteredOrders[i];
                const qty = Number(order.halfPortion) + Number(order.fullPortion);
                const totalPrice = qty * Number(order.price);

                let actionButton = "";
                let isUpdateDisabled = "";
                let isDeleteDisabled = "";
                let rowClass = "";
                let statusText = "";

                if (order.orderStatusId === 1) {
                    actionButton = `<button class="btn btn-success btn-sm accept-order-row-btn" data-id="${order.id}">Accept</button>`;
                    isUpdateDisabled = "";
                    isDeleteDisabled = "";
                    rowClass = "bg-warning";
                    statusText = "Assigning Order";
                } else if (order.orderStatusId === 2) {
                    const acceptedTime = orderAcceptedTimes[order.id];
                    const isOverdue =
                        acceptedTime && Date.now() - acceptedTime > 10 * 60 * 1000;

                    if (isOverdue) {
                        actionButton = `<button class="btn btn-danger btn-sm complete-order-row-btn" data-id="${order.id}">Complete (OVERDUE)</button>`;
                        rowClass = "bg-danger text-white";
                        statusText = "OVERDUE!";
                    } else {
                        actionButton = `<button class="btn btn-primary btn-sm complete-order-row-btn" data-id="${order.id}">Complete</button>`;
                        rowClass = "bg-info text-white";
                        statusText = "Active";
                    }

                    isUpdateDisabled = "";
                    isDeleteDisabled = "";
                }

                detailsHtml += `
<tr class="${rowClass}">
    
    <td style="display:none">${order.id}</td>

    <td class="item-name">${order.itemName}</td>

    <!-- HALF -->
    <td>
        <div class="qty-group d-flex align-items-center justify-content-center" data-id="${order.id}">
            <button class="btn btn-sm btn-light half-dec" ${isUpdateDisabled}>-</button>
            <span class="mx-2 half-val">${order.halfPortion}</span>
            <button class="btn btn-sm btn-light half-inc" ${isUpdateDisabled}>+</button>
        </div>
    </td>

    <!-- FULL -->
    <td>
        <div class="qty-group d-flex align-items-center justify-content-center" data-id="${order.id}">
            <button class="btn btn-sm btn-light full-dec" ${isUpdateDisabled}>-</button>
            <span class="mx-2 full-val">${order.fullPortion}</span>
            <button class="btn btn-sm btn-light full-inc" ${isUpdateDisabled}>+</button>
        </div>
    </td>

    <!-- TOTAL -->
    <td>₹${totalPrice}</td>

    <!-- STATUS -->
    <td>${statusText}</td>

    <!-- DATE -->
    <td>
        ${order.date
                        ? new Date(order.date).toLocaleString("en-IN", {
                            year: "numeric",
                            month: "2-digit",
                            day: "2-digit",
                            hour: "2-digit",
                            minute: "2-digit",
                            second: "2-digit",
                            hour12: false
                        })
                        : ""
                    }
    </td>

    <!-- SPECIAL INSTRUCTION -->
    <td>${order.specialInstructions || '—'}</td>

    <!-- ACTION -->
    <td>
        ${actionButton}
    </td>

    <!-- DELETE -->
    <td>
        <button class="btn btn-danger btn-sm delete-order-btn" data-id="${order.id}">
            Delete
        </button>
    </td>

</tr>
`;
            }
        }

        detailsHtml += `
                </tbody>
            </table>
        `;

        if (totalPages > 1) {
            detailsHtml += `<nav><ul class="pagination justify-content-end pagination-sm">`;
            detailsHtml += `<li class="page-item${currentPage === 1 ? " disabled" : ""
                }"><a class="page-link" href="#" data-page="${currentPage - 1}">Previous</a></li>`;
            for (let p = 1; p <= totalPages; p++) {
                detailsHtml += `<li class="page-item${p === currentPage ? " active" : ""
                    }"><a class="page-link" href="#" data-page="${p}">${p}</a></li>`;
            }
            detailsHtml += `<li class="page-item${currentPage === totalPages ? " disabled" : ""
                }"><a class="page-link" href="#" data-page="${currentPage + 1}">Next</a></li>`;
            detailsHtml += `</ul></nav>`;
        }

        $("#orderDetails").html(detailsHtml);

        // Pagination events
        $(document)
            .off("click", ".pagination .page-link")
            .on("click", ".pagination .page-link", function (e) {
                e.preventDefault();
                const page = parseInt($(this).data("page"));
                if (!isNaN(page) && page > 0 && page <= totalPages) {
                    currentPage = page;
                    renderTable(filteredOrders);
                }
            });
    }

    renderTable(tableOrders);

    $(document)
        .off("click", ".delete-order-btn")
        .on("click", ".delete-order-btn", function () {
            const id = $(this).data("id");
            let order = (window.liveOrdersData || []).find((o) => o.id === id);

            const status = Number(order?.orderStatusId);
            if (!order || (status !== 1 && status !== 2)) return;

            const reason = prompt("Enter reason for deleting this order:");

            if (!reason || reason.trim() === "") {
                alert("Reason is required.");
                return;
            }

            // Step 3: Confirm
            if (confirm("Are you sure you want to delete this order?")) {
                deleteOrder(id, reason);
            }
        });

    // Quantity adjust (only assigning)
    $(document)
        .off("click", ".qty-dec")
        .on("click", ".qty-dec", function () {
            const group = $(this).closest(".qty-group");
            const id = group.data("id");
            const input = group.find(".qty-input");
            let order = (window.liveOrdersData || []).find((o) => o.id === id);
            if (!order || order.orderStatusId !== 1) return;

            const currentValue =
                order.pendingFullPortion !== undefined
                    ? order.pendingFullPortion
                    : order.fullPortion;
            if (currentValue > 0) {
                order.pendingFullPortion = currentValue - 1;
                input.val(order.pendingFullPortion);
                markOrderAsModified(order);
            }
        });

    $(document)
        .off("click", ".qty-inc")
        .on("click", ".qty-inc", function () {
            const group = $(this).closest(".qty-group");
            const id = group.data("id");
            const input = group.find(".qty-input");
            let order = (window.liveOrdersData || []).find((o) => o.id === id);
            if (!order || order.orderStatusId !== 1) return;

            order.pendingFullPortion =
                (order.pendingFullPortion !== undefined
                    ? order.pendingFullPortion
                    : order.fullPortion) + 1;
            input.val(order.pendingFullPortion);
            markOrderAsModified(order);
        });

    updateConfirmOrderBtn(parseInt($(".modal-title").text().replace("Table", "")));
}

// Accept single row
$(document).on("click", ".accept-order-row-btn", function () {
    const id = $(this).data("id");
    let order = (window.liveOrdersData || []).find((o) => o.id === id);
    if (!order || order.orderStatusId !== 1) return;

    if (order.pendingFullPortion !== undefined && order.pendingFullPortion !== order.fullPortion) {
        updateOrderQuantity(order, function () {
            acceptOrder(order, function () {
                updateOrderDetails($(".modal-title").text());
                //checkAndStopBeepIfNoProblems();
            });
        });
    } else {
        acceptOrder(order, function () {
            updateOrderDetails($(".modal-title").text());
            //checkAndStopBeepIfNoProblems();
        });
    }
});

// Complete single row -> open payment modal
$(document).on("click", ".complete-order-row-btn", function () {
    const id = $(this).data("id");
    let order = (window.liveOrdersData || []).find((o) => o.id === id);

    if (!order || order.orderStatusId !== 2) {
        alert("Invalid order or order status");
        return;
    }

    openPaymentModal(id, order);
});

// ========== Payment Modal ==========

$(document).on("click", ".payment-option", function (e) {
    e.stopPropagation();
    $(".payment-option").removeClass("selected");
    $(this).addClass("selected");

    const modeAttr = $(this).attr("data-mode");
    const modeData = $(this).data("mode");
    selectedPaymentMode = modeAttr ?? modeData ?? null;

    $("#confirmPayment").prop("disabled", !selectedPaymentMode);
    $("#paymentError").toggleClass("active", !selectedPaymentMode);
});

$(document).on("click", "#cancelPayment", function () {
    $(discountInput).val('');
    $('#paymentDiscount').text('₹0.00');
    $('#paymentSubtotal').text('₹0.00');
    $('#paymentFinal').text('₹0.00');

    // Reset modal/payment state
    //window.currentModalOrderTotal = 0;
    //selectedPaymentMode = null;
    //currentOrderId = null;
    //currentOrderData = null;
    //currentOnlineOrderId = null;
    //paymentContext = null;

    // Reset UI affordances
    //$(".payment-option").removeClass("selected");
    //$("#confirmPayment").prop("disabled", true);
    //$("#paymentError").removeClass("active");
    closePaymentModal();
});
// Bind and update payment summary when user tabs out (blur) or clicks the discount input.
// Handles absolute rupee values and "NN%" percent entries.
function updatePaymentSummaryFromDiscountInput() {
    try {
        const discountEl = $('#discountInput');
        if (!discountEl.length) return;

        const subtotal = computeCurrentSubtotal() || 0;
        let raw = String(discountEl.val() ?? '').trim();

        let discountAmt = 0;
        if (raw.endsWith('%')) {
            // percent entry like "10%"
            const pct = parseFloat(raw.replace('%', '').replace(/[^0-9.\-]/g, '')) || 0;
            discountAmt = Math.floor(subtotal * (pct / 100));
        } else {
            // absolute rupee value, allow "₹" or commas
            const parsed = parseFloat(raw.replace(/[^\d.\-]/g, ''));
            discountAmt = isNaN(parsed) ? 0 : parsed;
        }

        // clamp
        if (discountAmt < 0) discountAmt = 0;
        if (discountAmt > subtotal) discountAmt = subtotal;

        // normalize discount input to absolute amount (in rupees)
        discountEl.val(String(discountAmt));

        // update UI summary elements
        $('#paymentDiscount').text(`₹${discountAmt.toFixed(2)}`);
        $('#paymentSubtotal').text(`₹${subtotal.toFixed(2)}`);
        const finalAmount = Math.max(0, subtotal - discountAmt);
        $('#paymentFinal').text(`₹${finalAmount.toFixed(2)}`);
    } catch (e) {
        console.warn('updatePaymentSummaryFromDiscountInput error', e);
    }
}

// Bind events: blur (tab out), Enter key, and click (select on focus)
$(document).off('blur', '#discountInput').on('blur', '#discountInput', function () {
    updatePaymentSummaryFromDiscountInput();
});
$(document).off('keydown', '#discountInput').on('keydown', '#discountInput', function (e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        $(this).blur();
    }
});
$(document).off('click', '#discountInput').on('click', '#discountInput', function (e) {
    e.stopPropagation();
    // select current value for quick replace
    $(this).select();
});
$(document).off("click", "#confirmPayment").on("click", "#confirmPayment", function () {


    if (!selectedPaymentMode) {
        $("#paymentError").addClass("active");
        return;
    }

    if (paymentContext === 'online') {
        handleOnlinePaymentConfirm();
    } else {
        handleTablePaymentConfirm();
    }
});


// Helper: find order index by orderId (tolerant matching)
function findOrderIndexByOrderId(orderId) {

    if (orderId === undefined || orderId === null) return -1;

    const key = String(orderId).trim();

    return ordersData.findIndex(o => {

        if (!o) return false;

        // Match common shapes: o.orderId, o.order_id, o.id (string or numeric)

        const candidates = [o.orderId, o.order_id, o.id].map(x => x === undefined || x === null ? "" : String(x));

        return candidates.some(c => c === key || c === key.replace(/^ORD_/, '') || c === ('ORD_' + key));

    });

}

// Remove entire order by orderId (returns true if removed)

function removeOrderByOrderId(orderId) {

    const idx = findOrderIndexByOrderId(orderId);

    if (idx === -1) return false;

    ordersData.splice(idx, 1);

    renderOrders();

    updateNewOrdersBadge();

    return true;

}
function normalizeKeyForMatch(raw) {
    if (raw === undefined || raw === null) return "";
    const s = String(raw).trim();
    // without ORD_ prefix, and base part before any trailing underscores
    const withoutPrefix = s.replace(/^ORD_/, "");
    const base = withoutPrefix.split("_")[0];
    return {
        full: s,
        withoutPrefix: withoutPrefix,
        base: base
    };
}

// Helper: find order index by orderId (tolerant matching, handles variants like "ORD_12345_0_2")
function findOrderIndexByOrderId(orderId) {
    if (orderId === undefined || orderId === null) return -1;
    const key = normalizeKeyForMatch(orderId);

    return ordersData.findIndex(o => {
        if (!o) return false;

        const candidates = [o.orderId, o.order_id, o.id].map(x => x === undefined || x === null ? "" : String(x));
        for (let cand of candidates) {
            if (!cand) continue;
            const c = normalizeKeyForMatch(cand);

            if (c.full === key.full || c.withoutPrefix === key.withoutPrefix || c.base === key.base) return true;

            if (("ORD_" + c.withoutPrefix) === key.full || ("ORD_" + key.withoutPrefix) === c.full) return true;

            if (c.full.includes(key.withoutPrefix) || key.withoutPrefix.includes(c.withoutPrefix)) return true;

            if (!isNaN(Number(c.base)) && !isNaN(Number(key.base)) && Number(c.base) === Number(key.base)) return true;
        }

        return false;
    });
}

function removeOrderByOrderId(orderId) {
    const idx = findOrderIndexByOrderId(orderId);
    if (idx === -1) return false;

    try {
        ordersData.splice(idx, 1);
        renderOrders();
        if (typeof updateNewOrdersBadge === "function") updateNewOrdersBadge();
        return true;
    } catch (e) {
        console.warn("removeOrderByOrderId failed:", e);
        return false;
    }
}
// Robust removal used in online confirm flow

function removeOnlineOrderByOrderId(orderId) {
    // Try direct removal first
    if (removeOrderByOrderId(orderId)) return true;
    // Sometimes orderId could be numeric or prefixed; try matching by last part
    const key = String(orderId).trim();
    const matchedIdx = ordersData.findIndex(o => {

        if (!o) return false;

        const candidates = [o.orderId, o.order_id, o.id].map(x => x === undefined || x === null ? "" : String(x));

        return candidates.some(c => c.includes(key) || key.includes(c));

    });

    if (matchedIdx !== -1) {

        ordersData.splice(matchedIdx, 1);

        renderOrders();

        updateNewOrdersBadge();

        return true;

    }

    return false;

}

//  handleOnlinePaymentConfirm

function handleOnlinePaymentConfirm() {

    const mode = selectedPaymentMode;
    const order = ordersData.find(o => String(o.orderId) === String(currentOnlineOrderId));

    if (!order) {

        showNotification("Online order not found", "error");

        return;

    } else {

        // Remove the entire order row from local ordersData where orderId matches

        try {

            const removed = removeOnlineOrderByOrderId(currentOnlineOrderId);

            if (!removed) {

                ordersData = ordersData.filter(o => String(o.orderId) !== String(currentOnlineOrderId));

                renderOrders();

                updateNewOrdersBadge();

            }

        } catch (e) {

            console.warn("Failed to remove order from ordersData:", e);

        }

    }

    // 2) Calculate discount + final amount

    let totalAmount = Number(order.total) || 0;

    let discount = 0;

    const discountEl = $("#discountInput");

    if (discountEl && discountEl.length) {

        const raw = discountEl.val() ?? "";

        const parsed = parseInt(String(raw).replace(/[^0-9]/g, ""), 10);

        discount = isNaN(parsed) ? 0 : parsed;

    }

    if (discount < 0) discount = 0;

    if (discount > totalAmount) discount = totalAmount;

    const finalAmount = totalAmount - discount;

    // 3) Prepare summary object for DB

    const summaryData = {

        OrderId: order.orderId,

        CustomerName: order.customer || "",

        Phone: order.phone || "",

        TotalAmount: totalAmount,

        DiscountAmount: discount,

        FinalAmount: finalAmount,

        PaymentMode: mode

    };
    if (!confirm("Are you sure you want to complete this order?")) {

        return;

    }

    $("#confirmPayment").prop("disabled", true);
    $.ajax({
        url: "/Repository/SaveOrderSummaryOnline",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(summaryData),
        success: function () {
            // 5) Update main order status in DB (your existing function)

            updateRestaurantOrderStatus(order.orderId, "Delivered");

            // Store payment mode locally (optional)

            order.paymentMode = mode;


            closePaymentModal();

            $("#confirmPayment").prop("disabled", false);

        },

        error: function () {

            showNotification("Error saving payment details!", "error");

            $("#confirmPayment").prop("disabled", false);

        }

    });

}


//This is for the table order items o increase karne ka tarika 
// FULL +
$(document).on("click", ".full-inc", function () {
    const id = $(this).closest(".qty-group").data("id");
    let order = window.liveOrdersData.find(o => o.id === id);
    if (!order) return;

    order.fullPortion = Number(order.fullPortion) + 1;

    updateOrderQuantityAPI(order);
    updateOrderDetails($(".modal-title").text());
});

// FULL -
$(document).on("click", ".full-dec", function () {
    const id = $(this).closest(".qty-group").data("id");
    let order = window.liveOrdersData.find(o => o.id === id);
    if (!order) return;

    order.fullPortion = Math.max(0, Number(order.fullPortion) - 1);

    updateOrderQuantityAPI(order);
    updateOrderDetails($(".modal-title").text());
});

// HALF +
$(document).on("click", ".half-inc", function () {
    const id = $(this).closest(".qty-group").data("id");
    let order = window.liveOrdersData.find(o => o.id === id);
    if (!order) return;

    order.halfPortion = Number(order.halfPortion) + 1;

    updateOrderQuantityAPI(order);
    updateOrderDetails($(".modal-title").text());
});

// HALF -
$(document).on("click", ".half-dec", function () {
    const id = $(this).closest(".qty-group").data("id");
    let order = window.liveOrdersData.find(o => o.id === id);
    if (!order) return;

    order.halfPortion = Math.max(0, Number(order.halfPortion) - 1);

    updateOrderQuantityAPI(order);
    updateOrderDetails($(".modal-title").text());
});


//This is the actual api to increase the items or to decrease the item 
function updateOrderQuantityAPI(order) {
    $.ajax({
        url: "/Repository/UpdateOrderQuantity",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify({
            id: order.id,
            halfPortion: order.halfPortion,
            fullPortion: order.fullPortion
        }),
        success: function () {
            console.log("Updated");
        },
        error: function () {
            alert("Update failed");
        }
    });
}

// handleTablePaymentConfirm
function handleTablePaymentConfirm() {

    const mode = selectedPaymentMode;

    const ids = (currentOrderData || [])
        .map((x) => {
            if (typeof x === "object" && x !== null) return x.id;
            return x;
        })
        .filter(Boolean);

    if (ids.length === 0) {
        safeCall("showSuccessMessage", "No orders selected to complete!");
        return;
    }

    if (!confirm("Are you sure you want to complete this order?")) {
        return;
    }

    $("#confirmPayment").prop("disabled", true);
    closePaymentModal();

    const ordersToComplete = ids
        .map((id) => (window.liveOrdersData || []).find((o) => o.id === id))
        .filter((o) => o);

    if (ordersToComplete.length === 0) {
        safeCall("showSuccessMessage", "No matching live orders found to complete!");
        $("#confirmPayment").prop("disabled", false);
        return;
    }

    let totalAmount = ordersToComplete.reduce((sum, item) => {
        let qty = Number(item.halfPortion) + Number(item.fullPortion);
        return sum + qty * Number(item.price);
    }, 0);

    let discount = 0;
    const discountEl = $("#discountInput");
    if (discountEl && discountEl.length) {
        const raw = discountEl.val() ?? "";
        const parsed = parseInt(String(raw).replace(/[^0-9]/g, ""), 10);
        discount = isNaN(parsed) ? 0 : parsed;
    }
    if (discount < 0) discount = 0;
    if (discount > totalAmount) {
        discount = totalAmount;
        if (discountEl && discountEl.length) discountEl.val(String(discount));
        safeCall("showSuccessMessage", "Discount reduced to total amount.");
    }

    let finalAmount = totalAmount - discount;
    if (finalAmount < 0) finalAmount = 0;

    let first = ordersToComplete[0];
    let summaryData = {
        OrderId: first.orderId,
        CustomerName: first.customerName || "",
        Phone: first.phone || "",
        TotalAmount: totalAmount,
        DiscountAmount: discount,
        FinalAmount: finalAmount,
        PaymentMode: mode
    };

    $.ajax({
        url: "/Repository/SaveOrderSummary",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(summaryData),
        success: function () {
            ordersToComplete.forEach((order) => {
                if (order) {
                    order.paymentMode = mode;
                    completeTableOrder(order, function () {
                        updateOrderDetails($(".modal-title").text());
                    });
                }
            });

            $("#confirmPayment").prop("disabled", false);
        },
        error: function () {
            safeCall("showSuccessMessage", "Error saving summary!");
            $("#confirmPayment").prop("disabled", false);
        }
    });

}
// Close on overlay click
$(document).on("click", "#paymentModal", function (e) {
    if (e.target.id === "paymentModal") {
        closePaymentModal();
    }
});

function openPaymentModal(orderId, orderData) {
    paymentContext = 'table';
    $("#divInProgressModal").modal("hide");
    $("#divInProgressModalHome").modal("hide");
    //setTimeout(() => {
    $("#paymentModal").addClass("active");
    const d = document.getElementById("discountInput");
    if (d) d.focus();
    //}, 250);


    currentOrderId = orderId;
    //currentOrderData = [orderData.id];
    currentOrderData = [orderData.orderId];
    selectedPaymentMode = null;

    $(".payment-option").removeClass("selected");
    $("#confirmPayment").prop("disabled", true);
    $("#paymentError").removeClass("active");

    $("#paymentModal").addClass("active");
}
function openOnlinePaymentModal(orderId) {

    paymentContext = 'online';
    currentOnlineOrderId = orderId;
    selectedPaymentMode = null;

    // Reset UI
    $(".payment-option").removeClass("selected");
    $("#discountInput").val('');
    $("#confirmPayment").prop("disabled", true);
    $("#paymentError").removeClass("active");

    // Just open the payment modal (no table modal here)
    $("#paymentModal").addClass("active");
    const d = document.getElementById("discountInput");
    if (d) d.focus();
}


function closePaymentModal() {
    $("#paymentModal").removeClass("active");
    selectedPaymentMode = null;
    currentOrderId = null;
    currentOrderData = null;

    currentOnlineOrderId = null;
    paymentContext = null;
}



function updateOrderQuantity(order, callback) {
    stopAllBeeps();

    const payload = {
        id: order.id,
        tableNo: order.tableNo,
        itemName: order.itemName,
        halfPortion: order.halfPortion,
        fullPortion: order.pendingFullPortion,
        price: order.price,
        orderStatusId: order.orderStatusId,
        OrderStatus: order.orderStatusId === 1 ? "Assigning Order" : "Active",
        date: order.date,
        isActive: order.isActive,
        paymentMode: order.paymentMode || null
    };

    $.ajax({
        url: "/Repository/UpdateOrderItem",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(payload),
        success: function () {
            order.fullPortion = order.pendingFullPortion;
            delete order.pendingFullPortion;
            if (callback) callback();
        },
        error: function () {
            safeCall("showSuccessMessage", "Failed to update order quantity!");
        }
    });
}

function markOrderAsModified(order) {
    // Fixed selector
    const row = $(`.qty-group[data-id="${order.id}"]`).closest("tr");
    row.addClass("border-primary border-2");
}

function acceptOrder(order, callback) {
    const payload = {
        id: order.id,
        tableNo: order.tableNo,
        itemName: order.itemName,
        halfPortion: order.halfPortion,
        fullPortion:
            order.pendingFullPortion !== undefined
                ? order.pendingFullPortion
                : order.fullPortion,
        price: order.price,
        orderStatusId: 2,
        OrderStatus: "Active",
        date: order.date,
        isActive: order.isActive,
        orderId: order.orderId
    };

    $.ajax({
        url: "/Repository/UpdateOrderItem",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(payload),
        success: function () {
            // Update local data
            order.orderStatusId = 2;
            if (order.pendingFullPortion !== undefined) {
                order.fullPortion = order.pendingFullPortion;
                delete order.pendingFullPortion;
            }

            // Record acceptance time
            orderAcceptedTimes[order.id] = Date.now();

            // Clear assigning timers
            if (reminderTimers[order.id]) {
                clearTimeout(reminderTimers[order.id]);
                delete reminderTimers[order.id];
            }
            if (orderReceivedTimes[order.id]) {
                delete orderReceivedTimes[order.id];
            }

            // Setup 10-min completion + 5-min repeats
            if (completionTimers[order.id]) {
                clearTimeout(completionTimers[order.id]);
            }
            completionTimers[order.id] = setTimeout(function completionReminder() {
                const stillActive = (window.liveOrdersData || []).some(
                    (o) => o.id === order.id && o.orderStatusId === 2
                );
                if (stillActive) {
                    startTableBeep(order.tableNo);
                    playBeep();
                    completionTimers[order.id] = setTimeout(completionReminder, 5 * 60 * 1000);
                } else {
                    clearTimeout(completionTimers[order.id]);
                    delete completionTimers[order.id];
                    delete orderAcceptedTimes[order.id];
                }
            }, 10 * 60 * 1000);

            refreshOrders();
            bindDynamicTable();
            updateStats();

            if (callback) callback();
        },
        error: function () {
            safeCall("showSuccessMessage", "Failed to accept order!");
        }
    });
}


// ---- Discount Button Handlers ---- //

function setupDiscountButtons() {
    $("#disciount5").click(() => applyPercentageDiscount(5));
    $("#disciount10").click(() => applyPercentageDiscount(10));
    $("#disciount15").click(() => applyPercentageDiscount(15));
}

function computeCurrentSubtotal() {
    try {
        // Online order context
        if (paymentContext === 'online' && currentOnlineOrderId) {
            const order = (ordersData || []).find(o => String(o.orderId) === String(currentOnlineOrderId));
            if (!order) return 0;
            if (!isNaN(Number(order.total)) && Number(order.total) > 0) {
                return Number(order.total);
            }
            if (Array.isArray(order.items)) {
                return order.items.reduce((s, it) => {
                    const qty = Number(it.quantity) || ((Number(it.fullPortion) || 0) + (Number(it.halfPortion) || 0));
                    return s + (Number(it.price || 0) * qty);
                }, 0);
            }
            return 0;
        }

        const ids = Array.isArray(currentOrderData) ? currentOrderData.map(x => {
            if (x && typeof x === 'object') return x.id ?? x.orderId ?? x;
            return x;
        }) : [];

        if (ids.length > 0) {
            const idStrings = ids.map(x => String(x));
            const orders = (window.liveOrdersData || []).filter(o =>
                idStrings.some(id => String(o.id) === id || String(o.orderId) === id)
            );
            return orders.reduce((sum, item) => {
                const qty = (Number(item.halfPortion) || 0) + (Number(item.fullPortion) || 0) || (Number(item.quantity) || 0);
                return sum + qty * (Number(item.price) || 0);
            }, 0);
        }

        // Fallback: try window.currentModalOrderTotal if set
        if (!isNaN(Number(window.currentModalOrderTotal)) && Number(window.currentModalOrderTotal) > 0) {
            return Number(window.currentModalOrderTotal);
        }

        return 0;
    } catch (e) {
        console.warn('computeCurrentSubtotal error', e);
        return 0;
    }
}

function applyPercentageDiscount(percent) {
    const subtotal = computeCurrentSubtotal() || 0;
    const discountamt = Math.floor(subtotal * (Number(percent) / 100)) || 0;

    // Keep canonical subtotal for the modal
    window.currentModalOrderTotal = subtotal;

    // Write values into the UI
    $('#discountInput').val(discountamt);
    $('#paymentDiscount').text(`₹${discountamt.toFixed(2)}`);
    $('#paymentSubtotal').text(`₹${subtotal.toFixed(2)}`);
    const finalAmount = Math.max(0, subtotal - discountamt);
    $('#paymentFinal').text(`₹${finalAmount.toFixed(2)}`);
    if (paymentContext === 'online') {
        if (!currentOnlineOrderId) {
            alert("No online order selected!");
            return;
        }

        const order = ordersData.find(o => String(o.orderId) === String(currentOnlineOrderId));
        if (!order) {
            alert("Online order not found!");
            return;
        }
        debugger
        const totalAmount = Number(order.total) || 0;
        const discountAmount = Math.floor((totalAmount * percent) / 100);
        $("#discountInput").val(discountAmount);
        return;
    }


    if (!currentOrderData || currentOrderData.length === 0) {
        alert("No orders to apply discount!");
        return;
    }


    const ids = (currentOrderData || []).map(x => {
        if (x && typeof x === "object") return x.id ?? x.orderId ?? x;
        return x;
    });

    const orders = (window.liveOrdersData || []).filter(o => {

        return ids.some(id => String(id) === String(o.id) || String(id) === String(o.orderId));
    });

    if (!orders || orders.length === 0) {
        alert("No matching live orders found to apply discount!");
        return;
    }

    let totalAmount = orders.reduce((sum, item) => {
        let qty = Number(item.halfPortion) + Number(item.fullPortion);
        return sum + qty * Number(item.price);
    }, 0);

    let discountAmount = Math.floor((totalAmount * percent) / 100);
    $("#discountInput").val(discountAmount);
}
function completeOrder(order, callback) {
    stopBeep();

    const payload = {
        id: order.id,
        tableNo: order.tableNo,
        itemName: order.itemName,
        halfPortion: order.halfPortion,
        fullPortion:
            order.pendingFullPortion !== undefined
                ? order.pendingFullPortion
                : order.fullPortion,
        price: order.price,
        orderStatusId: 3,
        OrderStatus: "Completed Today",
        date: order.date,
        isActive: order.isActive,
        orderId: order.orderId,
        paymentMode: order.paymentMode
    };

    $.ajax({
        url: "/Repository/UpdateOrderItem",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(payload),
        success: function () {
            // Move to history
            orderHistory.push({
                ...order,
                orderStatusId: 3,
                completedAt: new Date().toISOString()
            });

            // Remove from live
            window.liveOrdersData = (window.liveOrdersData || []).filter(
                (o) => o.id !== order.id
            );

            // Cleanup timers
            if (completionTimers[order.id]) {
                clearTimeout(completionTimers[order.id]);
                delete completionTimers[order.id];
            }
            if (orderAcceptedTimes[order.id]) {
                delete orderAcceptedTimes[order.id];
            }

            refreshOrders();
            bindDynamicTable();
            updateStats();

            //setTimeout(() => {
            //    checkAndStopBeepIfNoProblems();
            //}, 100);

            if (callback) callback();
        },
        error: function () {
            safeCall("showSuccessMessage", "Failed to complete order!");
        }
    });
}
// Table complete order 
function completeTableOrder(order, callback) {
    stopBeep();

    const payload = {
        id: order.id,
        tableNo: order.tableNo,
        itemName: order.itemName,
        halfPortion: order.halfPortion,
        fullPortion:
            order.pendingFullPortion !== undefined
                ? order.pendingFullPortion
                : order.fullPortion,
        price: order.price,
        orderStatusId: 3,
        OrderStatus: "Completed Today",
        date: order.date,
        isActive: order.isActive,
        orderId: order.orderId,
        paymentMode: order.paymentMode
    };

    $.ajax({
        url: "/Repository/UpdateTableOrderItem",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(payload),
        success: function () {
            orderHistory.push({
                ...order,
                orderStatusId: 3,
                completedAt: new Date().toISOString()
            });

            window.liveOrdersData = (window.liveOrdersData || []).filter(
                (o) => o.id !== order.id
            );

            if (completionTimers[order.id]) {
                clearTimeout(completionTimers[order.id]);
                delete completionTimers[order.id];
            }
            if (orderAcceptedTimes[order.id]) {
                delete orderAcceptedTimes[order.id];
            }

            refreshOrders();
            bindDynamicTable();
            updateStats();

            //setTimeout(() => {
            //    checkAndStopBeepIfNoProblems();
            //}, 100);

            if (callback) callback();
        },
        error: function () {
            safeCall("showSuccessMessage", "Failed to complete order!");
        }
    });
}

//function updateConfirmOrderBtn(tableNo) {
//    // If there are active orders -> "Complete" mode, else "Accept" mode
//    const data = window.liveOrdersData || [];
//    const hasActive = data.some((o) => o.tableNo === tableNo && o.orderStatusId === 2);
//    const $btn = $("#confirmOrderBtn");

//    if (!$btn.length) return;

//    if (hasActive) {
//        $btn.text("Complete Orders").data("mode", "complete").removeClass("btn-success").addClass("btn-primary");
//    } else {
//        $btn.text("Accept Orders").data("mode", "accept").removeClass("btn-primary").addClass("btn-success");
//    }
//}

// ========== Safe Stubs for external functions ==========

function safeCall(fnName, ...args) {
    if (typeof window[fnName] === "function") {
        try {
            return window[fnName](...args);
        } catch (e) {
            console.warn(`Error in ${fnName}:`, e);
        }
    }
    return undefined;
}

// If not defined elsewhere, provide harmless no-ops
if (typeof window.showSuccessMessage !== "function") {
    window.showSuccessMessage = function (msg) {
    };
}
if (typeof window.refreshOrders !== "function") {
    window.refreshOrders = function () {
        // no-op
    };
}
if (typeof window.updateStats !== "function") {
    window.updateStats = function () {
        // no-op
    };
}

if (typeof window.updateNewOrdersBadge !== "function") {
    window.updateNewOrdersBadge = function () {

    };
}
if (typeof window.deleteOrder !== "function") {
    window.deleteOrder = function (id) {
        // Implement your delete call here if needed

    };
}
// Refresh DataTable after changes
function refreshOrders() {
    if (liveOrdersTable) {
        liveOrdersTable.clear().rows.add(window.liveOrdersData || []).draw();
    }
}

// Update stats (active, total, new orders)
function updateStats() {
    const data = window.liveOrdersData || [];
    const activeOrders = data.filter(order => order.orderStatusId === 1 || order.orderStatusId === 2).length;
    const completedOrders = orderHistory.length;
    const newOrders = data.filter(order => order.orderStatusId === 1).length;

    $('#activeOrders').text(activeOrders);
    $('#totalOrders').text(completedOrders);
    $('#newOrdersBadge').text(newOrders);
}

// Show a toast message
function showSuccessMessage(message) {
    const toast = $(`
        <div class="alert alert-success alert-dismissible fade show position-fixed" 
             style="top: 100px; right: 20px; z-index: 9999; min-width: 300px;">
            <i class="fas fa-check-circle"></i> ${message}
            <button type="button" class="close" data-dismiss="alert">
                <span>&times;</span>
            </button>
        </div>
    `);

    $('body').append(toast);
    //setTimeout(() => {
    toast.alert('close');
    //}, 3000);
}


function deleteOrder(id, reason) {
    if (id === undefined || id === null) return;

    const idStr = String(id).trim();
    const payload = JSON.stringify({
        id: id,
        reason: reason || ""
    });

    // Helper: remove item locally
    function removeItemLocally(orderIndex, itemId) {
        const order = ordersData[orderIndex];

        order.items = order.items.filter(it =>
            String(it.itemId ?? it.id ?? it.item_id ?? "") !== String(itemId)
        );

        order.total = order.items.reduce((s, it) =>
            s + (Number(it.price || 0) * Number(it.quantity || 0)), 0
        );

        if (order.items.length === 0) ordersData.splice(orderIndex, 1);

        renderOrders();
        if (typeof updateNewOrdersBadge === 'function') updateNewOrdersBadge();
    }

    // =========================
    // 1) ITEM DELETE
    // =========================
    for (let i = 0; i < ordersData.length; i++) {
        const ord = ordersData[i];
        if (!ord || !Array.isArray(ord.items)) continue;

        const foundItem = ord.items.find(it => {
            const iid = it.itemId ?? it.id ?? it.item_id ?? "";
            return String(iid) === idStr;
        });

        if (foundItem) {
            $.ajax({
                url: '/Repository/SoftDeleteOrder',
                type: 'POST',
                contentType: 'application/json',
                data: payload,
                success: function () {
                    removeItemLocally(i, id);
                    showSuccessMessage('Order item deleted');
                },
                error: function (jqXHR) {
                    console.error('Item delete failed', jqXHR.responseText);
                    showNotification('Failed to delete item', 'error');
                }
            });
            return;
        }
    }

    // =========================
    // 2) ONLINE ORDER DELETE
    // =========================
    const orderIdx = ordersData.findIndex(o => {
        if (!o) return false;

        const candidates = [o.orderId, o.order_id, o.id]
            .map(x => x === undefined || x === null ? "" : String(x));

        return candidates.some(c =>
            c === idStr || c.includes(idStr) || idStr.includes(c)
        );
    });

    if (orderIdx !== -1) {
        $.ajax({
            url: '/Home/RejectOnlineOrder',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: payload,
            success: function () {
                ordersData.splice(orderIdx, 1);
                renderOrders();
                updateNewOrdersBadge();
                showSuccessMessage(`Online order ${idStr} removed`);
            },
            error: function (jqXHR) {
                console.error('Online delete failed', jqXHR.responseText);
                showNotification('Failed to remove online order', 'error');
            }
        });
        return;
    }

    // =========================
    // 3) TABLE ORDER DELETE
    // =========================
    $.ajax({
        url: '/Repository/SoftDeleteOrder',
        type: 'POST',
        contentType: 'application/json',
        data: payload,
        success: function (data) {

            // Remove from live orders
            window.liveOrdersData = (window.liveOrdersData || []).filter(order =>
                String(order.id) !== String(id) &&
                String(order.orderId || "") !== String(id)
            );

            // Cleanup timers
            try {
                if (reminderTimers[id]) { clearTimeout(reminderTimers[id]); delete reminderTimers[id]; }
                if (completionTimers[id]) { clearTimeout(completionTimers[id]); delete completionTimers[id]; }
                if (orderReceivedTimes[id]) delete orderReceivedTimes[id];
                if (orderAcceptedTimes[id]) delete orderAcceptedTimes[id];
            } catch (e) {
                console.warn('Timer cleanup error', e);
            }

            // Refresh UI
            refreshOrders();
            bindDynamicTable();
            updateStats();

            try {
                updateOrderDetails($('.modal-title').text());
            } catch (e) { }

            showSuccessMessage(data ? 'Deleted: ' + data : 'Order deleted successfully!');
        },
        error: function (jqXHR) {
            console.error('Delete failed', jqXHR.responseText);
            showNotification('Failed to delete order', 'error');
        }
    });
}
function updateConfirmOrderBtn(tableNo) {


    const $btn = $('#confirmOrderBtn');

    const data = window.liveOrdersData || [];

    const tableOrders = data.filter(order => order.tableNo === tableNo && order.orderStatusId !== 3);

    const hasAssigning = tableOrders.some(order => order.orderStatusId === 1);

    const hasActive = tableOrders.some(order => order.orderStatusId === 2);

    if (hasAssigning) {

        $btn.text('Accept Order').data('mode', 'accept').removeClass('btn-primary').addClass('btn-success');

    } else if (hasActive) {

        $btn.text('Complete Order').data('mode', 'complete').removeClass('btn-success').addClass('btn-primary');

    } else {

        $btn.text('Accept Order').data('mode', 'accept').removeClass('btn-primary').addClass('btn-success');

    }

}

let isZomatoActive = false;
let isSwiggyActive = false;

function initializeConnectionUI() {
    updateZomatoUI();
    updateSwiggyUI();
}

function updateZomatoUI() {
    if (isZomatoActive) {
        $('#zomatoBtn')
            .removeClass('connect')
            .addClass('disconnect')
            .text('Disconnect Zomato');

        $('#zomatoStatus')
            .text('Connected')
            .removeClass('disconnected sync-offline')
            .addClass('connected sync-online');
    } else {
        $('#zomatoBtn')
            .removeClass('disconnect')
            .addClass('connect')
            .text('Connect Zomato');

        $('#zomatoStatus')
            .text('Disconnected')
            .removeClass('connected sync-online')
            .addClass('disconnected sync-offline');
    }
}

function updateSwiggyUI() {
    if (isSwiggyActive) {
        $('#swiggyBtn')
            .removeClass('connect')
            .addClass('disconnect')
            .text('Disconnect Swiggy');

        $('#swiggyStatus')
            .text('Connected')
            .removeClass('disconnected sync-offline')
            .addClass('connected sync-online');
    } else {
        $('#swiggyBtn')
            .removeClass('disconnect')
            .addClass('connect')
            .text('Connect Swiggy');

        $('#swiggyStatus')
            .text('Disconnected')
            .removeClass('connected sync-online')
            .addClass('disconnected sync-offline');
    }
}

//FIXED: Use consistent property name 'orderId' instead of 'id'
let ordersData = [
    //{
    //    orderId: "ORD_1001",
    //    platform: "online",
    //    customer: "Rohit Sharma",
    //    phone: "9876543210",
    //    address: "Bandra, Mumbai",
    //    items: [
    //        { name: "Veg Crispy", quantity: 1, price: 170 },
    //        { name: "Fried Rice", quantity: 1, price: 150 }
    //    ],
    //    total: 320,
    //    status: "new",
    //    timestamp: new Date(),
    //    deliveryTime: "15 min"
    //},
    //{
    //    orderId: "ORD_1002",
    //    platform: "zomato",
    //    customer: "Rahul Verma",
    //    phone: "9123456780",
    //    address: "Andheri East, Mumbai",
    //    items: [
    //        { name: "Paneer Tikka", quantity: 2, price: 180 }
    //    ],
    //    total: 360,
    //    status: "confirmed",
    //    timestamp: new Date(),
    //    deliveryTime: "25 min"
    //},
    //{
    //    orderId: "ORD_1003",
    //    platform: "swiggy",
    //    customer: "Kiran Kumar",
    //    phone: "9001122334",
    //    address: "Vashi, Navi Mumbai",
    //    items: [
    //        { name: "Chicken Biryani", quantity: 1, price: 250 }
    //    ],
    //    total: 250,
    //    status: "preparing",
    //    timestamp: new Date(),
    //    deliveryTime: "30 min"
    //},
    //{
    //    orderId: "ORD_1004",
    //    platform: "coffee",
    //    customer: "Sneha Patil",
    //    phone: "9988776655",
    //    address: "Pickup",
    //    items: [
    //        { name: "Cold Coffee", quantity: 1, price: 120 }
    //    ],
    //    total: 120,
    //    status: "new",
    //    timestamp: new Date(),
    //    deliveryTime: "5 min"
    //}
];


let currentFilter = 'all';

function renderOrders() {


    const container = $('#ordersContainer');
    container.empty();

    // Filter orders based on selected platform
    const filteredOrders = currentFilter === 'all' ?
        ordersData : ordersData.filter(order => order.platform === currentFilter);

    // Create and append order cards
    filteredOrders.forEach(order => {
        const orderCard = createOrderCard(order);
        container.append(orderCard);
    });
}
function createOrderCard(order) {



    const platformClass = order.platform;
    const badgeClass = `badge-${order.platform}`;
    const statusClass = `status-${order.status}`;

    return `
        <div class="order-card ${platformClass}">
            <div class="order-header">
                <div class="order-id">#${order.orderId}</div>
                <span class="platform-badge ${badgeClass}">
                    ${order.platform.toUpperCase()}
                </span>
            </div>
            <div class="order-info mb-3">
                <div><strong>Customer:</strong> ${order.customer}</div>
                <div><strong>Phone:</strong> ${order.phone}</div>
                <div><strong>Delivery Time:</strong> ${order.deliveryTime}</div>
                <div><strong>Platform:</strong> ${order.platform.charAt(0).toUpperCase() + order.platform.slice(1)}</div>
                <div><strong>Notes:</strong> ${order.specialInstructions || '-'}</div>
            </div>
          <div class="order-items">
    ${order.items.map(item => `
    <div class="order-item" style="display:flex; justify-content:space-between; align-items:center;">
        <div>
            <strong>${item.name}</strong>
            <div style="font-size:0.9rem; color:#666;">
                ${item.fullPortion ? `<span>${item.fullPortion} × Full</span>` : ''}
                ${item.halfPortion ? `${item.fullPortion ? ' · ' : ''}<span>${item.halfPortion} × Half</span>` : ''}
            </div>
        </div>
        <div style="display:flex; align-items:center; gap:8px;">
            <span>₹${item.price}</span>
            <span class="badge badge-secondary">Qty ${item.quantity}</span>
            <button class="btn btn-sm btn-danger" onclick="deleteOrder(${item.itemId})" style="margin-left:10px;">✖</button>
        </div>
    </div>
`).join('')}
</div>


            <div class="order-footer">
                <div class="order-total">₹${order.total}</div>
                <span class="status-badge ${statusClass}">${order.status ? order.status.toUpperCase() : 'PENDING'}</span>
            </div>
            <div class="action-buttons">
                <button class="btn btn-primary btn-action" onclick="viewOrderDetails('${order.orderId}')">
                    <i class="fas fa-eye"></i> View
                </button>
                
                ${/* COFFEE ORDER BUTTONS */ ''}
                ${order.platform === 'coffee' ? `

    ${order.status === 'new' ? `
        <button class="btn btn-success btn-action" onclick="updateCoffeeOrderStatus('${order.orderId}','confirmed')">
            <i class="fas fa-check"></i> Accept
        </button>

        <button class="btn btn-danger btn-action" onclick="RejectCoffeeOrder('${order.orderId}')">
            <i class="fas fa-times"></i> Reject
        </button>
    ` : ''}

    ${order.status === 'confirmed' ? `
        <button class="btn btn-info btn-action" onclick="updateCoffeeOrderStatus('${order.orderId}','completed')">
            <i class="fas fa-coffee"></i> Mark Delivered
        </button>
    ` : ''}

    ${order.status === 'completed' ? `
        <span class="text-success font-weight-bold">
            <i class="fas fa-check-double"></i> Delivered
        </span>
    ` : ''}

` : ''}
                
                ${/* REGULAR RESTAURANT ORDERS */ ''}

                
                ${order.platform === 'restaurant' || order.platform === 'Online' ? `
                    ${order.status === 'Order Placed' ? `
                        <button class="btn btn-success btn-action" onclick="updateOrderStatusOnPlatform('${order.orderId}', 'Order In Progress', 'Online')">
                            <i class="fas fa-check"></i> Accept
                        </button>
                        <button class="btn btn-danger btn-action" onclick="rejectOrder('${order.orderId}')">
                            <i class="fas fa-times"></i> Reject
                        </button>
                    ` : ''}
                    ${order.status === 'Order In Progress' ? `
                        <button class="btn btn-warning btn-action" onclick="selectDeliveryStaff('${order.orderId}')">
                            <i class="fas fa-utensils"></i> Ready To Deliver
                        </button>
                        <button class="btn btn-danger btn-action" onclick="rejectOrder('${order.orderId}')">
                            <i class="fas fa-times"></i> Reject
                        </button>
                    ` : ''}
                    ${order.status === 'Out for delivery' ? `
                            <button class="btn btn-info btn-action" 
                            onclick="openOnlinePaymentModal('${order.orderId}')">
                            <i class="fas fa-check-circle"></i> Mark Delivered
                            </button>
`                                    : ''}

                   

                ` : ''}
                
                ${/* ZOMATO/SWIGGY ORDERS */ ''}
                ${order.platform === 'zomato' || order.platform === 'swiggy' ? `
                    ${order.status === 'new' ? `
                        <button class="btn btn-success btn-action" onclick="updateOrderStatus('${order.orderId}', 'confirmed')">
                            <i class="fas fa-check"></i> Accept
                        </button>
                        <button class="btn btn-danger btn-action" onclick="rejectOrder('${order.orderId}')">
                            <i class="fas fa-times"></i> Reject
                        </button>
                    ` : ''}
                    ${order.status === 'confirmed' ? `
                        <button class="btn btn-warning btn-action" onclick="updateOrderStatus('${order.orderId}', 'preparing')">
                            <i class="fas fa-utensils"></i> Preparing
                        </button>
                    ` : ''}
                    ${order.status === 'preparing' ? `
                        <button class="btn btn-info btn-action" onclick="updateOrderStatus('${order.orderId}', 'ready')">
                            <i class="fas fa-check-circle"></i> Ready
                        </button>
                    ` : ''}
                    ${order.status === 'ready' ? `
                        <button class="btn btn-success btn-action" onclick="updateOrderStatus('${order.orderId}', 'completed')">
                            <i class="fas fa-shipping-fast"></i> Mark Delivered
                        </button>
                    ` : ''}
                    ${order.status === 'completed' ? `
                        <span class="text-success font-weight-bold">
                            <i class="fas fa-check-double"></i> Completed
                        </span>
                    ` : ''}
                ` : ''}
            </div>
        </div>
    `;
}

// FILTER ORDERS FUNCTION
function filterOrders(platform) {
    currentFilter = platform;

    $('.platform-tab').removeClass('active');
    $(`.platform-tab[data-platform="${platform}"]`).addClass('active');

    renderOrders();
}


function selectDeliveryStaff(orderId) {
    selectedDeliveryOrderId = orderId;

    $.ajax({
        url: '/Staff/GetAll',
        method: 'GET',
        success: function (data) {
            debugger
            data = data.filter(x => x.avalFDelivery === true);
            let dropdown = $('#staffDropdown');
            dropdown.empty();

            dropdown.append(`<option value="">Select Staff</option>`);

            data.forEach(s => {
                dropdown.append(`<option value="${s.staffId}">${s.fullName}</option>`);
            });

            $('#staffModal').show();
        },
        error: function () {
            alert("Failed to load staff");
        }
    });
}
function confirmDelivery() {
    const staffId = $('#staffDropdown').val();

    if (!staffId) {
        alert("Select staff");
        return;
    }

    updateRestaurantOrderStatus(
        selectedDeliveryOrderId,
        'Out for delivery',
        staffId
    );

    closeStaffModal();
}

function closeStaffModal() {
    $('#staffModal').hide();
}
function updateOrderStatus(orderId, newStatus) {


    const orderIndex = ordersData.findIndex(o => o.orderId === orderId);
    if (orderIndex !== -1) {
        ordersData[orderIndex].status = newStatus;

        renderOrders();
        updateNewOrdersBadge();

        showNotification(`Order #${orderId} updated to ${newStatus}`, 'success');


        updateOrderStatusOnPlatform(orderId, newStatus, ordersData[orderIndex].platform);

        //getOrdersFromRestaurant();

    }
}

// VIEW ORDER DETAILS FUNCTION  
function viewOrderDetails(orderId) {


    stopBeep();
    stopAllTableBeeps();
    const order = ordersData.find(o => o.orderId === orderId);
    if (!order) return;

    // Update modal title
    //$('.modal-title').first().html(`Order #${order.orderId} ${order.platform.toUpperCase()}</span>`);

    const content = `
        <div class="row">
            <div class="col-md-6">
                <h6>Customer Information</h6>
                <p><strong>Name:</strong> ${order.customer}</p>
                <p><strong>Phone:</strong> ${order.phone}</p>
                <p><strong>Address:</strong> ${order.address}</p>
                <p><strong>Delivery Time:</strong> ${order.deliveryTime}</p>
                <p><strong>Status:</strong> <span class="status-badge status-${order.status}">${order.status.toUpperCase()}</span></p>
                <p><strong>Order Time:</strong> ${order.timestamp.toLocaleString()}</p>
            </div>
            <div class="col-md-6">
                <h6>Order Details</h6>
                <div class="table-responsive">
                    <table class="table table-sm">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th>Qty</th>
                                <th>Price</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${order.items.map(item => `
                                <tr>
                                    <td>${item.name}</td>
                                    <td>${item.quantity}</td>
                                    <td>₹${item.price}</td>
                                </tr>
                            `).join('')}
                            <tr class="font-weight-bold">
                                <td colspan="2">Total</td>
                                <td>₹${order.total}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    `;

    $('#orderDetailsHome').html(content);
    $('#divInProgressModalHome').modal('show');
}

// UPDATE NEW ORDERS BADGE
function updateNewOrdersBadge() {
    const newOrdersCount = ordersData.filter(order => order.status === 'new').length;
    $('#newOrdersBadge').text(newOrdersCount);
}

// GET ORDERS FROM ZOMATO
//function getOrdersFromZomato() {
//    if (!isZomatoActive) {
//        console.log('Zomato is disconnected. No new orders will be fetched.');
//        return;
//    }

//    $.ajax({
//        url: '/api/zomato/orders',
//        method: 'GET',
//        headers: {
//            'Authorization': 'Bearer YOUR_ZOMATO_API_KEY',
//            'Content-Type': 'application/json'
//        },
//        success: function (data) {
//            console.log('Zomato orders fetched successfully');

//            const zomatoOrders = data.map(order => ({
//                orderId: order.zomato_order_id,
//                platform: 'zomato',
//                customer: order.customer_name,
//                phone: order.customer_phone,
//                address: order.address,
//                items: order.items.map(item => ({
//                    name: item.dish_name,
//                    quantity: item.quantity,
//                    price: item.price
//                })),
//                total: order.total_amount,
//                status: mapZomatoStatus(order.status),
//                timestamp: new Date(order.created_at),
//                deliveryTime: order.estimated_delivery_time
//            }));

//            const existingIds = ordersData.map(order => order.orderId);
//            const newOrders = zomatoOrders.filter(order => !existingIds.includes(order.orderId));

//            ordersData = [...newOrders, ...ordersData];
//            renderOrders();
//            updateNewOrdersBadge();

//            if (newOrders.length > 0) {
//                showNotification(`${newOrders.length} new Zomato orders received!`, 'success');
//            }
//        },
//        error: function (xhr, status, error) {
//            console.error('Zomato API Error:', error);
//            showNotification('Failed to fetch Zomato orders', 'error');
//            $('#zomatoStatus').removeClass('sync-online').addClass('sync-offline');
//        }
//    });
//}

// GET ORDERS FROM SWIGGY
//function getOrdersFromSwiggy() {
//    if (!isSwiggyActive) {
//        console.log('Swiggy is disconnected. No new orders will be fetched.');
//        return;
//    }

//    $.ajax({
//        url: '/api/swiggy/orders',
//        method: 'GET',
//        headers: {
//            'Authorization': 'Bearer YOUR_SWIGGY_API_KEY',
//            'Content-Type': 'application/json'
//        },
//        success: function (data) {
//            console.log('Swiggy orders fetched successfully');

//            const swiggyOrders = data.orders.map(order => ({
//                orderId: order.order_id,
//                platform: 'swiggy',
//                customer: order.customer_details.name,
//                phone: order.customer_details.phone,
//                items: order.order_items.map(item => ({
//                    name: item.item_name,
//                    quantity: item.quantity,
//                    price: item.item_price
//                })),
//                total: order.order_total,
//                status: mapSwiggyStatus(order.order_status),
//                timestamp: new Date(order.order_time),
//                deliveryTime: order.delivery_time_estimate
//            }));

//            const existingIds = ordersData.map(order => order.orderId);
//            const newOrders = swiggyOrders.filter(order => !existingIds.includes(order.orderId));

//            ordersData = [...newOrders, ...ordersData];
//            renderOrders();
//            updateNewOrdersBadge();

//            if (newOrders.length > 0) {
//                showNotification(`${newOrders.length} new Swiggy orders received!`, 'success');
//            }
//        },
//        error: function (xhr, status, error) {
//            console.error('Swiggy API Error:', error);
//            showNotification('Failed to fetch Swiggy orders', 'error');
//            $('#swiggyStatus').removeClass('sync-online').addClass('sync-offline');
//        }
//    });
//}

// REFRESH ALL PLATFORMS
function refreshAllPlatforms() {
    $('#zomatoStatus').removeClass('sync-offline').addClass('sync-online');
    $('#swiggyStatus').removeClass('sync-offline').addClass('sync-online');
    $('#HomeStatus').removeClass('sync-offline').addClass('sync-online');

    //getOrdersFromRestaurant();
    //getOrdersFromZomato();
    //getOrdersFromSwiggy();
    getOrdersFromCoffee();
}
getOrdersFromCoffee();
// UPDATE ORDER STATUS ON PLATFORM
function updateOrderStatusOnPlatform(orderId, status, platform) {



    if (platform === 'zomato') {
        updateZomatoOrderStatus(orderId, status);
    } else if (platform === 'swiggy') {
        updateSwiggyOrderStatus(orderId, status);
    } else if (platform === 'Online') {

        updateRestaurantOrderStatus(orderId, status);
    }
}

// UPDATE ZOMATO ORDER STATUS
function updateZomatoOrderStatus(orderId, status) {
    $.ajax({
        url: '/Repository/UpdateOrderItem',
        method: 'PUT',
        headers: {
            'Authorization': 'Bearer YOUR_ZOMATO_API_KEY',
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({
            status: mapToZomatoStatus(status)
        }),
        success: function (data) {

            showNotification(`Zomato order #${orderId} status updated`, 'success');
        },
        error: function (xhr, status, error) {

            showNotification(`Failed to update Zomato order #${orderId}`, 'error');
        }
    });
}


function updateSwiggyOrderStatus(orderId, status) {
    $.ajax({
        url: `/api/swiggy/orders/${orderId}/status`,
        method: 'PUT',
        headers: {
            'Authorization': 'Bearer YOUR_SWIGGY_API_KEY',
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({
            order_status: mapToSwiggyStatus(status)
        }),
        success: function (data) {

            showNotification(`Swiggy order #${orderId} status updated`, 'success');
        },
        error: function (xhr, status, error) {

            showNotification(`Failed to update Swiggy order #${orderId}`, 'error');
        }
    });
}

// UPDATE RESTAURANT ORDER STATUS
function updateRestaurantOrderStatus(orderId, status, staffId = null) {

    const statusMap = {
        'Order In Progress': 2,
        'Out for delivery': 3,
        'Delivered': 4,
        'Removed': 5
    };

    const payload = {
        orderId: String(orderId),
        orderStatus: statusMap[status],
        deliveryStaffId: staffId
    };

    $.ajax({
        url: '/Repository/UpdateOnlineStatus',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function () {

            const idx = ordersData.findIndex(o => String(o.orderId) === String(orderId));
            if (idx !== -1) {
                ordersData[idx].status = status;
            }


            renderOrders();
            showNotification('Order updated', 'success');
        },
        error: function () {
            showNotification('Failed to update order', 'error');
        }
    });
}

function RejectCoffeeOrder(orderId) {
    if (!confirm('Are you sure you want to reject this order?')) {
        return;
    }

    const orderIndex = ordersData.findIndex(o => o.orderId === orderId);

    if (orderIndex !== -1) {

        const order = ordersData[orderIndex];

        $.ajax({
            url: "/Repository/RejectCoffeeOrder",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(orderId),
            success: function () {


                ordersData.splice(orderIndex, 1);

                renderOrders();
                updateNewOrdersBadge();
                showNotification(`Order #${orderId} has been rejected`, 'warning');
            },
            error: function () {
                showNotification(`Failed to reject order #${orderId}`, 'error');
            }
        });
    }
}


// STATUS MAPPING FUNCTIONS
function mapZomatoStatus(zomatoStatus) {
    const statusMap = {
        'placed': 'new',
        'accepted': 'confirmed',
        'cooking': 'preparing',
        'ready': 'ready',
        'dispatched': 'completed'
    };
    return statusMap[zomatoStatus] || 'new';
}

function mapSwiggyStatus(swiggyStatus) {
    const statusMap = {
        'ORDER_PLACED': 'new',
        'RESTAURANT_ACCEPTED': 'confirmed',
        'FOOD_PREPARATION': 'preparing',
        'READY_FOR_PICKUP': 'ready',
        'ORDER_DISPATCHED': 'completed'
    };
    return statusMap[swiggyStatus] || 'new';
}

function mapToZomatoStatus(internalStatus) {
    const statusMap = {
        'new': 'placed',
        'confirmed': 'accepted',
        'preparing': 'cooking',
        'ready': 'ready',
        'completed': 'dispatched'
    };
    return statusMap[internalStatus] || 'placed';
}

function mapToSwiggyStatus(internalStatus) {
    const statusMap = {
        'new': 'ORDER_PLACED',
        'confirmed': 'RESTAURANT_ACCEPTED',
        'preparing': 'FOOD_PREPARATION',
        'ready': 'READY_FOR_PICKUP',
        'completed': 'ORDER_DISPATCHED'
    };
    return statusMap[internalStatus] || 'ORDER_PLACED';
}

// AUTO REFRESH
function startAutoRefresh() {
    setInterval(() => {
        refreshAllPlatforms();
        if (Math.random() > 0.8) {
            const platforms = [];

            if (isZomatoActive) platforms.push('zomato');
            if (isSwiggyActive) platforms.push('swiggy');

            if (platforms.length === 0) {

                return;
            }

            const randomPlatform = platforms[Math.floor(Math.random() * platforms.length)];

            const newOrder = {
                orderId: randomPlatform.toUpperCase() + Date.now(),
                platform: randomPlatform,
                customer: "New Customer",
                phone: "+91 " + Math.floor(Math.random() * 9000000000 + 1000000000),
                items: [
                    { name: "Sample Item", quantity: 1, price: Math.floor(Math.random() * 300 + 100) }
                ],
                total: Math.floor(Math.random() * 500 + 200),
                status: "new",
                timestamp: new Date(),
                deliveryTime: Math.floor(Math.random() * 30 + 15) + " min"
            };

            ordersData.unshift(newOrder);
            renderOrders();
            updateNewOrdersBadge();
            showNotification(`New ${randomPlatform} order received!`, 'info');
        }
    }, 3000);
}

function showNotification(message, type) {
    const alertClass = type === 'success' ? 'alert-success' :
        type === 'info' ? 'alert-info' :
            type === 'error' ? 'alert-danger' : 'alert-warning';

    const notification = `
        <div class="alert ${alertClass} alert-dismissible fade show position-fixed" 
             style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
            ${message}
            <button type="button" class="close" data-dismiss="alert">
                <span>&times;</span>
            </button>
        </div>
    `;

    $('body').append(notification);

    //setTimeout(() => {
    $('.alert').fadeOut();
    //}, 3000);
}

function getOrdersFromRestaurant() {


    $.ajax({
        url: "/Home/GetOrderOnline",
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        },
        success: function (data) {

            const groupedOrders = data.reduce((acc, order) => {
                const orderKey = order.orderId;

                if (!acc[orderKey]) {
                    acc[orderKey] = {


                        orderId: order.orderId,
                        platform: order.orderType || 'online',
                        customer: order.customerName || 'Walk-in Customer',
                        phone: order.phone || 'N/A',
                        items: [],
                        total: 0,
                        status: order.orderStatus || 'new',
                        timestamp: new Date(order.date),
                        deliveryTime: order.prep_time || '15 min',
                        address: order.address,
                        specialInstructions: order.specialInstructions
                    };
                }

                acc[orderKey].items.push({
                    itemId: order.id,
                    name: order.itemName,
                    halfPortion: Number(order.halfPortion) || 0,
                    fullPortion: Number(order.fullPortion) || 0,
                    quantity: (Number(order.halfPortion) || 0) + (Number(order.fullPortion) || 0),
                    price: order.price
                });

                const itemTotal = (order.fullPortion || 0) * order.price + (order.halfPortion || 0) * order.price;
                acc[orderKey].total += itemTotal;

                return acc;
            }, {});

            const restaurantOrders = Object.values(groupedOrders);

            const existingOrderIds = ordersData.map(order => order.orderId);
            const newOrders = restaurantOrders.filter(order => !existingOrderIds.includes(order.orderId));

            ordersData = [...newOrders, ...ordersData];

            renderOrders();
            updateNewOrdersBadge();
            if (newOrders.length > 0) {
                playBeep();
            }

        },
        error: function (xhr, status, error) {

        }
    });
}

function getOrdersFromCoffee() {

    $.ajax({
        url: '/Repository/GetCoffeeOrders',
        method: 'GET',
        contentType: 'application/json',

        success: function (data) {

            const groupedOrders = {};

            data.forEach(order => {

                if (!groupedOrders[order.orderNumber]) {

                    groupedOrders[order.orderNumber] = {

                        orderId: order.orderNumber,
                        platform: 'coffee',
                        customer: order.customerName || 'Walk-in Customer',
                        phone: order.customerPhone || 'N/A',
                        items: [],
                        total: 0,
                        status: order.orderStatus,
                        timestamp: new Date(order.orderDate),
                        deliveryTime: '5-10 min',
                        address: 'Pickup'
                    };
                }

                groupedOrders[order.orderNumber].items.push({
                    itemId: order.id,
                    name: order.coffeeName,
                    quantity: order.quantity,
                    price: order.price
                });

                groupedOrders[order.orderNumber].total += order.price * order.quantity;

            });

            const coffeeOrders = Object.values(groupedOrders);

            const existingOrderIds = ordersData.map(o => String(o.orderId));

            const newOrders = coffeeOrders.filter(o =>
                !existingOrderIds.includes(String(o.orderId))
            );

            if (newOrders.length > 0) {

                ordersData.unshift(...newOrders);

                renderOrders();
                updateNewOrdersBadge();

                showNotification(`${newOrders.length} new coffee orders received!`, 'success');
            }
        },

        error: function () {
            showNotification('Failed to fetch coffee orders', 'error');
        }
    });
}


// Coffee Orders section 

function groupCoffeeOrders(data) {

    const groupedOrders = {};

    data.forEach(order => {

        if (!groupedOrders[order.orderNumber]) {

            groupedOrders[order.orderNumber] = {
                orderId: order.orderNumber,
                platform: 'coffee',
                customer: order.customerName || 'Walk-in Customer',
                phone: order.customerPhone || 'N/A',
                items: [],
                total: 0,
                status: order.orderStatus,
                timestamp: new Date(order.orderDate),
                deliveryTime: '5-10 min',
                address: 'Pickup'
            };
        }

        groupedOrders[order.orderNumber].items.push({
            itemId: order.id,
            name: order.coffeeName,
            quantity: order.quantity,
            price: order.price
        });

        groupedOrders[order.orderNumber].total += order.price * order.quantity;

    });

    return Object.values(groupedOrders);
}
function updateCoffeeOrderStatus(orderId, newStatus) {

    const orderIndex = ordersData.findIndex(o => String(o.orderId) === String(orderId));

    if (orderIndex === -1) {
        showNotification(`Order #${orderId} not found`, 'error');
        return;
    }

    const statusMap = {
        confirmed: 2,
        completed: 4
    };

    const payload = {
        orderId: orderId,
        status: statusMap[newStatus]
    };

    $.ajax({

        url: '/Repository/UpdateCoffeeOrderStatus',
        method: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(payload),

        success: function () {

            ordersData[orderIndex].status = newStatus;

            renderOrders();
            updateNewOrdersBadge();

            const messages = {
                confirmed: 'Order accepted successfully ☕',
                completed: 'Order delivered successfully ✅'
            };

            showNotification(messages[newStatus], 'success');

            if (newStatus === 'completed') {

                setTimeout(() => {

                    ordersData.splice(orderIndex, 1);
                    renderOrders();
                    updateNewOrdersBadge();

                }, 1000);
            }
        },

        error: function () {
            showNotification(`Failed to update order #${orderId}`, 'error');
        }
    });
}

// Accept Coffee Order (wrapper function)
function acceptCoffeeOrder(orderId) {
    if (!confirm('Accept this coffee order?')) {
        return;
    }
    updateCoffeeOrderStatus(orderId, 'confirmed');
}

// Deliver Coffee Order (wrapper function)
function deliverCoffeeOrder(orderId) {
    if (!confirm('Mark this coffee order as delivered?')) {
        return;
    }
    updateCoffeeOrderStatus(orderId, 'completed');
}


$(document).on('click', '#btnViewHistory', function () {
    $('#orderHistoryModal').modal('show');
    $('#orderHistoryContainer').html(`
      <div class="text-center p-4 text-muted">
          <i class="fas fa-spinner fa-spin"></i> Fetching order history...
      </div>
  `);
    //setTimeout(() => {
    renderOrderHistory();
    //}, 300);
});

//Auto refresh every 30 seconds

function printThermalBill(order) {

    const payload = {
        OrderId: order.orderId || "",
        Name: order.customer || "",
        Phone: order.phone || "",
        Address: order.address || "",
        OrderTime: order.timestamp ? new Date(order.timestamp) : new Date(),

        Items: (order.items || []).map(it => ({
            Name: it.name || it.itemName || "-",
            Quantity: it.quantity || ((it.fullPortion || 0) + (it.halfPortion || 0)) || 1,
            Price: Number(it.price || 0)
        })),

        Subtotal: Number(order.total) || 0,
        Discount: Number(order.discountAmount) || 0,
        Total: Number(order.total) || 0,

        PrinterName: "Everycom-58-Series"
    };

    fetch('/api/print/PrintBill', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    })
        .then(r => {
            if (!r.ok) throw new Error("Printer error");
            return r.json();
        })
        .then(res => {
            console.log("Printed:", res);
        })
        .catch(err => {
            console.error("Print failed:", err);
        });
}

(function () {
    // ── State ──────────────────────────────────────────────────────
    let _tableNo = null;
    let _categories = [];
    let _subcats = [];
    let _allItems = [];
    let _activeCatId = null;
    let _activeSubId = null;
    let _cart = {};
    let _loaded = false;

    // ── Public API ─────────────────────────────────────────────────
    window.openNewOrderModal = function (tableNo) {
        _tableNo = tableNo;
        document.getElementById('nomTableBadge').textContent = 'Table ' + tableNo;
        document.getElementById('newOrderModal').style.display = 'flex';
        document.body.style.overflow = 'hidden';
        _cart = {};
        document.getElementById('nomCustName').value = '';
        document.getElementById('nomCustPhone').value = '';
        document.getElementById('nomSpecialNote').value = '';
        updateSummary();
        if (!_loaded) loadMenu();
        else renderItems();
    };

    window.closeNewOrderModal = function () {
        document.getElementById('newOrderModal').style.display = 'none';
        document.body.style.overflow = '';
    };

    // ── Load Menu ──────────────────────────────────────────────────
    function loadMenu() {
        setLoading(true);
        Promise.all([
            fetch('/GetMenuCategory').then(r => r.json()),
            fetch('/GetMenuSubcategory').then(r => r.json()),
            fetch('/GetMenuItem').then(r => r.json())
        ]).then(function ([cats, subs, items]) {
            _categories = Array.isArray(cats) ? cats : [];
            _subcats = Array.isArray(subs) ? subs : [];
            _allItems = Array.isArray(items) ? items : [];
            _loaded = true;
            buildCategoryList();
            setLoading(false);
        }).catch(function (err) {
            console.error('Menu load failed', err);
            setLoading(false);
            document.getElementById('nomItemsGrid').innerHTML =
                '<div class="nom-no-results">Failed to load menu. Please try again.</div>';
        });
    }

    function setLoading(on) {
        if (on) {
            document.getElementById('nomItemsGrid').innerHTML =
                '<div class="nom-loading"><div class="nom-spinner"></div><span>Loading menu…</span></div>';
        }
    }

    // ── Categories ─────────────────────────────────────────────────
    function buildCategoryList() {
        const ul = document.getElementById('nomCatList');
        ul.innerHTML = '';

        // "All" entry
        const allLi = document.createElement('li');
        allLi.textContent = 'All Items';
        allLi.className = 'active';
        allLi.dataset.id = '';
        allLi.onclick = () => selectCategory(null, allLi);
        ul.appendChild(allLi);

        _categories.forEach(cat => {
            const li = document.createElement('li');
            li.textContent = cat.categoryName || cat.name || cat.CategoryName || 'Category';
            li.dataset.id = cat.id || cat.categoryId || cat.Id || '';
            li.onclick = () => selectCategory(li.dataset.id, li);
            ul.appendChild(li);
        });

        _activeCatId = null;
        buildSubcatStrip();
        renderItems();
    }

    function selectCategory(catId, el) {
        _activeCatId = catId || null;
        _activeSubId = null;
        document.querySelectorAll('#nomCatList li').forEach(l => l.classList.remove('active'));
        el.classList.add('active');
        buildSubcatStrip();
        renderItems();
    }

    // ── Subcategories ──────────────────────────────────────────────
    function buildSubcatStrip() {
        const strip = document.getElementById('nomSubcatStrip');
        strip.innerHTML = '';

        const subs = _activeCatId
            ? _subcats.filter(s => String(s.categoryId || s.CategoryId || s.catId || '') === String(_activeCatId))
            : _subcats;

        if (subs.length === 0) { strip.style.display = 'none'; return; }
        strip.style.display = 'flex';

        // All sub-cat button
        const allBtn = document.createElement('button');
        allBtn.className = 'nom-subcat-btn active';
        allBtn.textContent = 'All';
        allBtn.onclick = () => selectSubcat(null, allBtn);
        strip.appendChild(allBtn);

        subs.forEach(sub => {
            const btn = document.createElement('button');
            btn.className = 'nom-subcat-btn';
            btn.textContent = sub.subcategoryName || sub.subCategoryName || sub.name || sub.SubcategoryName || 'Sub';
            btn.dataset.id = sub.id || sub.subcategoryId || sub.SubcategoryId || '';
            btn.onclick = () => selectSubcat(btn.dataset.id, btn);
            strip.appendChild(btn);
        });
    }

    function selectSubcat(subId, el) {
        _activeSubId = subId || null;
        document.querySelectorAll('.nom-subcat-btn').forEach(b => b.classList.remove('active'));
        el.classList.add('active');
        renderItems();
    }

    // ── Items ──────────────────────────────────────────────────────
    function getFilteredItems() {
        const search = (document.getElementById('nomSearch').value || '').toLowerCase().trim();
        return _allItems.filter(item => {
            const name = (item.itemName || item.name || item.ItemName || '').toLowerCase();
            const catId = String(item.categoryId || item.CategoryId || item.catId || '');
            const subId = String(item.subcategoryId || item.SubcategoryId || item.subCategoryId || '');

            if (search && !name.includes(search)) return false;
            if (_activeCatId && catId !== String(_activeCatId)) return false;
            if (_activeSubId && subId !== String(_activeSubId)) return false;
            return true;
        });
    }

    function renderItems() {
        const grid = document.getElementById('nomItemsGrid');
        const items = getFilteredItems();

        if (items.length === 0) {
            grid.innerHTML = '<div class="nom-no-results">No items found.</div>';
            return;
        }

        grid.innerHTML = '';
        items.forEach(item => {
            const id = item.id || item.itemId || item.Id;
            const name = item.itemName || item.name || item.ItemName || 'Item';
            const price = Number(item.price || item.Price || item.fullPrice || 0);
            const halfP = Number(item.halfPrice || item.HalfPrice || (price * 0.5) || 0);
            const isVeg = (item.isVeg !== undefined ? item.isVeg : item.IsVeg) !== false;

            const cartEntry = _cart[id];
            const inCart = !!cartEntry;
            const fullQty = cartEntry ? cartEntry.full : 0;
            const halfQty = cartEntry ? cartEntry.half : 0;

            const card = document.createElement('div');
            card.className = 'nom-item-card' + (inCart ? ' in-cart' : '');
            card.innerHTML = `
        ${inCart ? '<div class="nom-cart-badge">' + (fullQty + halfQty) + '</div>' : ''}
        <div class="nom-item-top">
          <div class="nom-item-${isVeg ? 'veg' : 'nonveg'}"></div>
          <div class="nom-item-name">${name}</div>
        </div>
        <div class="nom-item-price">₹${price.toFixed(2)}</div>
        <div class="nom-portion-row">
          <button class="nom-portion-btn${fullQty > 0 ? ' has-qty' : ''}" onclick="nomAddPortion(${id},${price},'full',event)">
            <span>FULL</span>
            <span>${fullQty > 0 ? '× ' + fullQty : '₹' + price.toFixed(0)}</span>
          </button>
          ${halfP > 0 ? `
          <button class="nom-portion-btn${halfQty > 0 ? ' has-qty' : ''}" onclick="nomAddPortion(${id},${halfP},'half',event)">
            <span>HALF</span>
            <span>${halfQty > 0 ? '× ' + halfQty : '₹' + halfP.toFixed(0)}</span>
          </button>` : ''}
        </div>
      `;
            // Store item meta on element for reference
            card.dataset.itemId = id;
            grid.appendChild(card);
        });
    }

    // ── Cart operations ────────────────────────────────────────────
    window.nomAddPortion = function (itemId, price, portion, e) {
        e && e.stopPropagation();
        const item = _allItems.find(i => String(i.id || i.itemId || i.ItemId) === String(itemId));
        if (!item) return;

        if (!_cart[itemId]) {
            _cart[itemId] = { item, full: 0, half: 0, fullPrice: 0, halfPrice: 0 };
        }
        if (portion === 'full') {
            _cart[itemId].full++;
            _cart[itemId].fullPrice = price;
        } else {
            _cart[itemId].half++;
            _cart[itemId].halfPrice = price;
        }

        updateSummary();
        renderItems();
    };

    window.nomRemoveCartItem = function (itemId) {
        delete _cart[itemId];
        updateSummary();
        renderItems();
    };

    function updateSummary() {
        const cartContainer = document.getElementById('nomCartItems');
        const entries = Object.values(_cart).filter(e => e.full > 0 || e.half > 0);

        let total = 0;
        let count = 0;

        if (entries.length === 0) {
            cartContainer.innerHTML = `
        <div class="nom-cart-empty">
          <svg width="36" height="36" fill="none" stroke="#CBD5E1" stroke-width="1.5" viewBox="0 0 24 24">
            <circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/>
            <path d="M1 1h4l2.68 13.39a2 2 0 001.99 1.61h9.72a2 2 0 001.99-1.61L23 6H6"/>
          </svg>
          <p>No items yet</p>
        </div>`;
        } else {
            cartContainer.innerHTML = '';
            entries.forEach(entry => {
                const id = entry.item.id || entry.item.itemId || entry.item.Id;
                const name = entry.item.itemName || entry.item.name || entry.item.ItemName || '';
                const rowTotal = (entry.full * entry.fullPrice) + (entry.half * entry.halfPrice);
                total += rowTotal;
                count += entry.full + entry.half;

                const portions = [];
                if (entry.full > 0) portions.push(entry.full + ' Full');
                if (entry.half > 0) portions.push(entry.half + ' Half');

                const row = document.createElement('div');
                row.className = 'nom-cart-row';
                row.innerHTML = `
          <div class="nom-cart-row-info">
            <div class="nom-cart-row-name">${name}</div>
            <div class="nom-cart-row-portions">${portions.join(' + ')}</div>
          </div>
          <div class="nom-cart-row-price">₹${rowTotal.toFixed(2)}</div>
          <button class="nom-cart-remove" onclick="nomRemoveCartItem(${id})" title="Remove">
            <svg width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path d="M18 6L6 18M6 6l12 12"/>
            </svg>
          </button>`;
                cartContainer.appendChild(row);
            });
        }

        document.getElementById('nomSubtotal').textContent = '₹' + total.toFixed(2);
        document.getElementById('nomGrandTotal').textContent = '₹' + total.toFixed(2);
        document.getElementById('nomCartCount').textContent = count + (count === 1 ? ' item' : ' items');
        document.getElementById('nomCartTotal').textContent = '₹' + total.toFixed(2);
        document.getElementById('nomPlaceBtn').disabled = count === 0;
    }

    // ── Place Order ────────────────────────────────────────────────
    window.placeNewOrder = function () {
        const entries = Object.values(_cart).filter(e => e.full > 0 || e.half > 0);
        if (entries.length === 0) return;

        // Split each item into full-portion row and half-portion row separately
        // so each DB row has a single unit price. home.js card = (full + half) * price.
        const orderItems = [];
        entries.forEach(function (entry) {
            const itemId = Number(entry.item.id || entry.item.itemId || entry.item.Id);
            const fullUnitPrice = Number(entry.fullPrice || entry.item.price || entry.item.Price || 0);
            const halfUnitPrice = Number(entry.halfPrice || entry.item.price2 || Math.round(fullUnitPrice / 2) || 0);

            if (entry.full > 0) {
                orderItems.push({ item_id: itemId, full: entry.full, half: 0, Price: fullUnitPrice });
            }
            if (entry.half > 0) {
                orderItems.push({ item_id: itemId, full: 0, half: entry.half, Price: halfUnitPrice });
            }
        });

        const payload = {
            selectedTable: parseInt(_tableNo),
            orderItems: orderItems,
            customerName: document.getElementById('nomCustName').value.trim() || 'Walk-in',
            userPhone: document.getElementById('nomCustPhone').value.trim() || '',
            specialInstruction: document.getElementById('nomSpecialNote').value.trim() || 'No Instructions',
            OrderType: 'dine',               // lowercase 'dine' matches GetOrder filter
            paymentMode: 'Cash'
        };

        const btn = document.getElementById('nomPlaceBtn');
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<div class="nom-spinner" style="width:18px;height:18px;border-width:2px;border-color:rgba(255,255,255,.3);border-top-color:#fff"></div> Placing…';
        }

        $.ajax({
            url: '/Repository/SaveTableOrder',   // ← FIXED: was '/Repository/PlaceOrder' (404)
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function () {
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = '<svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg> Place Order';
                }
                showSuccessMessage('Order placed for Table ' + _tableNo + '!');
                closeNewOrderModal();
                if (typeof loadTableOrders === 'function') loadTableOrders();
                if (typeof loadTableCount === 'function') loadTableCount();
            },
            error: function (xhr) {
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = '<svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg> Place Order';
                }
                alert('Failed to place order: ' + (xhr.responseText || 'Server error'));
            }
        });
    }; window.placeNewOrder = function () {
        const entries = Object.values(_cart).filter(e => e.full > 0 || e.half > 0);
        if (entries.length === 0) return;

        const orderItems = entries.map(entry => ({
            full: entry.full,
            half: entry.half,
            item_id: Number(entry.item.id || entry.item.itemId || entry.item.Id),
            Price: Number(entry.fullPrice || entry.item.price || entry.item.Price || 0)
        }));

        const payload = {
            selectedTable: _tableNo,
            orderItems: orderItems,
            customerName: document.getElementById('nomCustName').value.trim() || 'Walk-in',
            userPhone: document.getElementById('nomCustPhone').value.trim() || '',
            specialInstruction: document.getElementById('nomSpecialNote').value.trim() || '',
            OrderType: 'DineIn',
            deliveryType: 'DineIn'
        };

        const btn = document.getElementById('nomPlaceBtn');
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<div class="nom-spinner" style="width:18px;height:18px;border-width:2px;border-color:rgba(255,255,255,.3);border-top-color:#fff"></div> Placing…';
        }

        $.ajax({
            url: '/Repository/SaveTableOrder',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function () {
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = '<svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg> Place Order';
                }
                showSuccessMessage('Order placed for Table ' + _tableNo + '!');
                closeNewOrderModal();

                if (typeof loadTableOrders === 'function') loadTableOrders();
                if (typeof loadTableCount === 'function') loadTableCount();
            },
            error: function (xhr) {
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = '<svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg> Place Order';
                }
                alert('Failed to place order: ' + (xhr.responseText || 'Server error'));
            }
        });
    };

    // Close on overlay click
    const _newOrderModal = document.getElementById('newOrderModal');
    if (_newOrderModal) {
        _newOrderModal.addEventListener('click', function (e) {
            if (e.target === this) closeNewOrderModal();
        });
    }

    // Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            const _m = document.getElementById('newOrderModal');
            if (_m && _m.style && _m.style.display !== 'none') {
                closeNewOrderModal();
            }
        }
    });

    // Replace the direct call with a safe binder
    function bindNomSearch() {
        const el = document.getElementById('nomSearch');
        if (el) {
            el.addEventListener('input', function () {
                if (_loaded) renderItems();
            });
        }
    }

    // If DOM not ready, wait; otherwise bind now
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindNomSearch);
    } else {
        bindNomSearch();
    }
})();