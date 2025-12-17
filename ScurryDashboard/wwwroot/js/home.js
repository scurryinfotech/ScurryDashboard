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

// ========== Utilities ==========

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
    if (isBeepPlaying) return;

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

$(document).on("click", "#stopBeepBtn", function () {
    stopAllBeeps();
});


$(document).ready(function () {

    loadTableOrders(false);

    setupDiscountButtons();
    setInterval(function () {
        loadTableOrders(false);

    }, 30000);

    setInterval(function () {
        getOrdersFromRestaurant();

    }, 20000);

    loadTableCount();


    safeCall("renderOrders");
    safeCall("updateNewOrdersBadge");


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

                        setTimeout(() => {
                            checkAndStopBeepIfNoProblems();
                        }, 100);

                        setTimeout(() => {
                            updateConfirmOrderBtn(tableNo);
                            updateOrderDetails(tableTitle);
                        }, 500);
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
        setTimeout(() => {
            $("#paymentModal").addClass("active");
            const d = document.getElementById("discountInput");
            if (d) d.focus();
        }, 250);
    });

    // When in-progress modal shows, set button mode
    $(document).on("show.bs.modal", "#divInProgressModal", function () {
        const tableTitle = $(this).find(".modal-title").text().trim();
        const tableNo = parseInt(tableTitle.replace("Table", ""));
        updateConfirmOrderBtn(tableNo);
    });
    $(document).on("show.bs.modal", "#divInProgressModalHome", function () {
        const tableTitle = $(this).find(".modal-title").text().trim();
        const tableNo = parseInt(tableTitle.replace("Table", ""));
        updateConfirmOrderBtn(tableNo);
    });
});

// ========== Data Loading ==========

function loadTableOrders() {
    $.ajax({
        url: "/home/GetOrder",
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
                                5 * 60 * 1000
                            );
                        } else {
                            clearTimeout(completionTimers[order.id]);
                            delete completionTimers[order.id];
                            delete orderAcceptedTimes[order.id];
                        }
                    }, msLeft);
                }
            });

            // Cleanup completion timers for orders no longer active
            Object.keys(completionTimers).forEach((orderId) => {
                if (!activeOrders.some((o) => o.id == orderId)) {
                    clearTimeout(completionTimers[orderId]);
                    delete completionTimers[orderId];
                    delete orderAcceptedTimes[orderId];
                }
            });

            // Cleanup received times and reminder timers for no-longer assigning
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
                    return elapsed > 10 * 60 * 1000;
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
                            // Next reminder in 5 minutes
                            reminderTimers[order.id] = setTimeout(reminder, 5 * 60 * 1000);
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
                    $("#divInProgressModalHome").modal("show");
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
        url: "/home/GetTableCount",
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

        tblhtml += `<div class="card text-white ${cardClass} m-3 card-click-animation" data-toggle="modal" data-target="#divInProgressModal">`;
        tblhtml += `<div class="card-body"><center><h5 class="card-title">Table ${tbl}</h5>`;
        tblhtml += `<div><strong>Total: ₹${totalPrice}</strong></div></center></div>`;
        tblhtml += `<div class="card-footer"><center><small>${statusText}</small></center></div>`;
        tblhtml += `</div>`;

        if (i % 3 == 2) tblhtml += "</div></div>";
    });

    if (arrTable.length % 3 !== 0) {
        tblhtml += "</div></div>";
    }

    $("#divTable").html(tblhtml);

    renderOrderHistory();
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
                    hour: "2-digit",
                    minute: "2-digit",
                    day: "2-digit",
                    month: "2-digit",
                    year: "numeric",
                    hour12: true
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

                    isUpdateDisabled = "disabled";
                    isDeleteDisabled = "disabled";
                }

                detailsHtml += `
                    <tr class="${rowClass}">
                        <td  style="display:none" >${order.id}</td>
                        <td class="item-name">${order.itemName}</td>
                        <td>${order.halfPortion}</td>
                        <td>${order.fullPortion}</td>
                        <td>₹${totalPrice}</td>
                        <td>${statusText}</td>
                        <td>${order.date
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
                    }</td>
                    <td>${order.specialInstructions} </td>
                        <td style="display:none">
                            <div class="input-group input-group-sm qty-group d-flex flex-row align-items-center" data-id="${order.id}">
                                <button class="btn btn-light qty-dec" type="button" ${isUpdateDisabled}>-</button>
                                <input class="form-control qty-input text-center" value="${order.pendingFullPortion !== undefined
                        ? order.pendingFullPortion
                        : order.fullPortion
                    }" readonly style="min-width:40px; max-width:50px;" ${isUpdateDisabled}>
                                <button class="btn btn-light qty-inc" type="button" ${isUpdateDisabled}>+</button>
                            </div>
                        </td>
                        <td>
                            ${actionButton}
                        </td>
                        <td>
                            <button class="btn btn-danger btn-sm delete-order-btn" data-id="${order.id}" ${isDeleteDisabled}>Delete</button>
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

    // Delete (only assigning)
    $(document)
        .off("click", ".delete-order-btn")
        .on("click", ".delete-order-btn", function () {
            const id = $(this).data("id");
            let order = (window.liveOrdersData || []).find((o) => o.id === id);
            if (!order || order.orderStatusId !== 1) return;
            if (confirm("Are you sure you want to delete this order?")) {
                deleteOrder(id);
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
                checkAndStopBeepIfNoProblems();
            });
        });
    } else {
        acceptOrder(order, function () {
            updateOrderDetails($(".modal-title").text());
            checkAndStopBeepIfNoProblems();
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
        url: "/home/SaveOrderSummaryOnline",
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
        url: "/home/SaveOrderSummary",
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
    setTimeout(() => {
        $("#paymentModal").addClass("active");
        const d = document.getElementById("discountInput");
        if (d) d.focus();
    }, 250);


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
        url: "/home/UpdateOrderItem",
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
        url: "/home/UpdateOrderItem",
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

// ---- Discount helpers: compute subtotal and bind buttons ---- //
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

        // Table context (currentOrderData contains ids or objects)
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
        url: "/home/UpdateOrderItem",
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

            setTimeout(() => {
                checkAndStopBeepIfNoProblems();
            }, 100);

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
        url: "/home/UpdateTableOrderItem",
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

            setTimeout(() => {
                checkAndStopBeepIfNoProblems();
            }, 100);

            if (callback) callback();
        },
        error: function () {
            safeCall("showSuccessMessage", "Failed to complete order!");
        }
    });
}

function updateConfirmOrderBtn(tableNo) {
    // If there are active orders -> "Complete" mode, else "Accept" mode
    const data = window.liveOrdersData || [];
    const hasActive = data.some((o) => o.tableNo === tableNo && o.orderStatusId === 2);
    const $btn = $("#confirmOrderBtn");

    if (!$btn.length) return;

    if (hasActive) {
        $btn.text("Complete Orders").data("mode", "complete").removeClass("btn-success").addClass("btn-primary");
    } else {
        $btn.text("Accept Orders").data("mode", "accept").removeClass("btn-primary").addClass("btn-success");
    }
}

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
        console.log("[INFO]", msg);
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
    setTimeout(() => {
        toast.alert('close');
    }, 3000);
}


function deleteOrder(id) {

    if (id === undefined || id === null) return;

    const idStr = String(id).trim();

    // Helper: remove item from ordersData and update UI

    function removeItemLocally(orderIndex, itemId) {

        const order = ordersData[orderIndex];

        order.items = order.items.filter(it => String(it.itemId ?? it.id ?? it.item_id ?? "") !== String(itemId));

        // Recalculate total

        order.total = order.items.reduce((s, it) => s + (Number(it.price || 0) * Number(it.quantity || 0)), 0);

        // If no items left remove the whole order

        if (order.items.length === 0) ordersData.splice(orderIndex, 1);

        renderOrders();

        if (typeof updateNewOrdersBadge === 'function') updateNewOrdersBadge();

    }

    // 1) Check if id matches an itemId inside any online order

    for (let i = 0; i < ordersData.length; i++) {

        const ord = ordersData[i];

        if (!ord || !Array.isArray(ord.items)) continue;

        const foundItem = ord.items.find(it => {

            const iid = it.itemId ?? it.id ?? it.item_id ?? "";

            return String(iid) === idStr;

        });

        if (foundItem) {

            // Call server to soft-delete this item (server endpoint expects { id: <itemId> })

            $.ajax({

                url: '/home/SoftDeleteOrder',

                type: 'POST',

                contentType: 'application/json',

                data: JSON.stringify(id),

                success: function (data, textStatus, jqXHR) {

                    console.log('SoftDeleteOrder item success', { id: id, status: jqXHR.status, data });

                    removeItemLocally(i, id);

                    showSuccessMessage('Order item deleted');

                    setTimeout(() => { checkAndStopBeepIfNoProblems(); }, 100);

                },

                error: function (jqXHR, textStatus, errorThrown) {

                    console.error('SoftDeleteOrder item failed', { id, status: jqXHR.status, body: jqXHR.responseText });

                    const message = jqXHR.responseText ? jqXHR.responseText : 'Failed to delete order item';

                    showNotification(message, 'error');

                }

            });

            return; // handled

        }

    }

    // 2) Check if id matches an entire online orderId

    const orderIdx = ordersData.findIndex(o => {

        if (!o) return false;

        const candidates = [o.orderId, o.order_id, o.id].map(x => x === undefined || x === null ? "" : String(x));

        return candidates.some(c => c === idStr || c.includes(idStr) || idStr.includes(c));

    });

    if (orderIdx !== -1) {

        const order = ordersData[orderIdx];

        // Use RejectOnlineOrder endpoint (controller has it) to remove online order server-side.

        $.ajax({

            url: '/Home/RejectOnlineOrder',

            type: 'POST',

            contentType: 'application/json; charset=utf-8',

            data: JSON.stringify(idStr),

            success: function (data, textStatus, jqXHR) {

                console.log('RejectOnlineOrder success', { orderId: idStr, status: jqXHR.status, data });

                // Remove locally only after server success

                ordersData.splice(orderIdx, 1);

                renderOrders();

                if (typeof updateNewOrdersBadge === 'function') updateNewOrdersBadge();

                showSuccessMessage(`Online order ${idStr} removed`);

            },

            error: function (jqXHR, textStatus, errorThrown) {

                console.error('RejectOnlineOrder failed', { orderId: idStr, status: jqXHR.status, body: jqXHR.responseText });

                const message = jqXHR.responseText ? jqXHR.responseText : `Failed to remove online order ${idStr}`;

                showNotification(message, 'error');

            }

        });

        return;

    }

    // 3) Fallback: assume this is a live/table order id (numeric) --> call SoftDeleteOrder and only update UI on success

    $.ajax({

        url: '/home/SoftDeleteOrder',

        type: 'POST',

        contentType: 'application/json',

        data: JSON.stringify(id),

        success: function (data, textStatus, jqXHR) {

            console.log('SoftDeleteOrder table success', { id, status: jqXHR.status, data });

            // Remove from local liveOrdersData if present (id may be numeric)

            window.liveOrdersData = (window.liveOrdersData || []).filter(order => {

                return String(order.id) !== String(id) && String(order.orderId || "") !== String(id);

            });

            // Clean up timers for this id

            try {

                if (reminderTimers[id]) { clearTimeout(reminderTimers[id]); delete reminderTimers[id]; }

                if (completionTimers[id]) { clearTimeout(completionTimers[id]); delete completionTimers[id]; }

                if (orderReceivedTimes[id]) delete orderReceivedTimes[id];

                if (orderAcceptedTimes[id]) delete orderAcceptedTimes[id];

            } catch (e) {

                console.warn('Timer cleanup error for id', id, e);

            }

            // Refresh UI

            refreshOrders();

            bindDynamicTable();

            updateStats();

            try { updateOrderDetails($('.modal-title').text()); } catch (e) { /* ignore */ }

            if (data) {

                showSuccessMessage('Deleted: ' + String(data));

            } else {

                showSuccessMessage('Order deleted successfully!');

            }

            setTimeout(() => { checkAndStopBeepIfNoProblems(); }, 100);
        },

        error: function (jqXHR, textStatus, errorThrown) {

            console.error('SoftDeleteOrder table failed', { id, status: jqXHR.status, body: jqXHR.responseText });

            const message = jqXHR.responseText ? jqXHR.responseText : 'Failed to delete order';

            showNotification(message, 'error');

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

$(document).ready(function () {
    initializeConnectionUI();
    getOrdersFromRestaurant();

    // Zomato button click handler
    $('#zomatoBtn').on('click', function () {
        isZomatoActive = !isZomatoActive;
        updateZomatoUI();
    });

    // Swiggy button click handler
    $('#swiggyBtn').on('click', function () {
        isSwiggyActive = !isSwiggyActive;
        updateSwiggyUI();
    });

    // Initialize orders and auto-refresh

    startAutoRefresh();
});

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
                <div><strong>Notes:</strong> ${order.specialInstructions}</div>
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
                        <button class="btn btn-success btn-action" onclick="acceptCoffeeOrder('${order.orderId}')">
                            <i class="fas fa-check"></i> Accept
                        </button>
                        <button class="btn btn-danger btn-action" onclick="rejectOrder('${order.orderId}')">
                            <i class="fas fa-times"></i> Reject
                        </button>
                    ` : ''}
                    ${order.status === 'confirmed' ? `
                        <button class="btn btn-info btn-action" onclick="deliverCoffeeOrder('${order.orderId}')">
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
                        <button class="btn btn-warning btn-action" onclick="updateOrderStatusOnPlatform('${order.orderId}', 'Out for delivery', 'Online')">
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

// UPDATE ORDER STATUS FUNCTION (SINGLE UNIFIED VERSION)
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
    //getOrdersFromCoffee();
}

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
        url: '/home/UpdateOrderItem',
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
function updateRestaurantOrderStatus(orderId, status) {


    const statusMap = {
        'Order In Progress': 2,
        'Out for delivery': 3,
        'Delivered': 4,
        'Removed': 5
    };

    // compute numericStatus first
    const numericStatus = statusMap[status] ?? null;


    const payload = {
        orderId: String(orderId),
        orderStatus: numericStatus,
        //paymentMode: mode  
    };



    $.ajax({
        url: '/Home/UpdateOnlineStatus',
        type: 'PUT',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(payload),
        success: function (data) {
            const idx = ordersData.findIndex(o => String(o.orderId) === String(orderId));
            if (idx !== -1) {

                ordersData[idx].status = status;
            }
            if (status === "Order In Progress") {
                printThermalBill(ordersData[idx]);
            }
            renderOrders();

            showNotification('Order updated', 'success');
        },
        error: function (xhr, statusText, error) {

            showNotification(`Failed to update order #${orderId}`, 'error');
        }
    });
}


function rejectOrder(orderId) {
    if (!confirm('Are you sure you want to reject this order?')) {
        return;
    }

    const orderIndex = ordersData.findIndex(o => o.orderId === orderId);

    if (orderIndex !== -1) {

        const order = ordersData[orderIndex];

        $.ajax({
            url: "/Home/RejectOnlineOrder",
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

    setTimeout(() => {
        $('.alert').fadeOut();
    }, 3000);
}

function getOrdersFromRestaurant() {


    $.ajax({
        url: "/home/GetOrderOnline",
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
            if (!isAnyAlarmPlaying()) {
                playBeep();
            }
        },
        error: function (xhr, status, error) {

        }
    });
}

window.restaurantDashboard = {
    viewOrderDetails,
    updateOrderStatus,
    filterOrders,
    rejectOrder,
    acceptCoffeeOrder,
    deliverCoffeeOrder,
    updateCoffeeOrderStatus
};


function getOrdersFromCoffee() {
    $.ajax({
        url: '/Home/GetCoffeeOrders',
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        },
        success: function (data) {


            const coffeeOrders = data.map(order => ({
                orderId: order.orderNumber,
                platform: 'coffee',
                customer: order.customerName || 'Walk-in Customer',
                phone: order.customerPhone || 'N/A',
                items: [{
                    name: order.coffeeName,
                    quantity: order.quantity,
                    price: order.price,
                    description: order.description
                }],
                total: order.totalPrice,
                status: 'new',
                timestamp: new Date(order.orderDate),
                deliveryTime: '5-10 min',
                address: 'Pickup'
            }));

            const existingOrderIds = ordersData.map(order => order.orderId);
            const newOrders = coffeeOrders.filter(order => !existingOrderIds.includes(order.orderId));

            ordersData = [...newOrders, ...ordersData];
            renderOrders();
            updateNewOrdersBadge();

            if (newOrders.length > 0) {
                showNotification(`${newOrders.length} new coffee orders received!`, 'success');
            }
        },
        error: function (xhr, status, error) {

            showNotification('Failed to fetch coffee orders', 'error');
        }
    });
}


// Coffee Orders section 


function updateCoffeeOrderStatus(orderId, newStatus) {
    renderOrders();

    const orderIndex = ordersData.findIndex(o => o.orderId === orderId);
    if (orderIndex === -1) {
        showNotification(`Order #${orderId} not found`, 'error');
        return;
    }

    const order = ordersData[orderIndex];


    const statusMap = {
        'confirmed': 'Accepted',
        'completed': 'Delivered'
    };

    const apiStatus = statusMap[newStatus];

    if (!apiStatus) {
        showNotification('Invalid status', 'error');
        return;
    }


    const payload = {
        orderId: orderId,
        status: apiStatus
    };


    $.ajax({
        url: '/Home/UpdateCoffeeOrderStatus',
        method: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(payload),
        success: function (response) {
            ordersData[orderIndex].status = newStatus;


            renderOrders();
            updateNewOrdersBadge();


            const messages = {
                'confirmed': ' Order accepted successfully',
                'completed': ' Order delivered successfully'
            };
            showNotification(messages[newStatus], 'success');


            if (newStatus === 'completed') {
                setTimeout(() => {
                    ordersData.splice(orderIndex, 1);
                    renderOrders();
                    updateNewOrdersBadge();
                }, 2000);
            }
        },
        error: function (xhr, status, error) {

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
    setTimeout(() => {
        renderOrderHistory();
    }, 300);
});

//Auto refresh every 30 seconds
//setInterval(function () {
//    getOrdersFromRestaurant();

//}, 3000);
function printThermalBill(order) {
    const billHTML = `
        <div style="font-family: monospace; padding:10px; width:280px;">
            <h3 style="text-align:center; margin:0;">Grill N Shakes</h3>
            <p style="text-align:center; margin:0;">Order Receipt</p>
            <hr>

            <p><strong>Order ID:</strong> ${order.orderId}</p>
            <p><strong>Name:</strong> ${order.customer}</p>
            <p><strong>Phone:</strong> ${order.phone}</p>
            ${order.address ? `<p><strong>Address:</strong> ${order.address}</p>` : ""}
            <p><strong>Order Time:</strong> ${order.timestamp.toLocaleString()}</p>

            <hr>

            <table style="width:100%; font-size:14px;">
                ${order.items
            .map(
                (item) =>
                    `<tr>
                          <td>${item.name}</td>
                          <td style="text-align:right;">x${item.quantity}</td>
                          <td style="text-align:right;">₹${item.price}</td>
                      </tr>`
            )
            .join("")}
            </table>

            <hr>

            <p style="text-align:right; font-size:16px;">
                <strong>Total: ₹${order.total}</strong>
            </p>
            <hr>
            <p style="text-align:center;">Thank you! Visit Again.</p>
        </div>
    `;

    const printWindow = window.open("", "", "width=300,height=600");
    printWindow.document.write(`
        <html>
            <head>
                <title>Print Bill</title>
                <style>body { margin:0; font-family: monospace; }</style>
            </head>
            <body>${billHTML}</body>
        </html>
    `);
    printWindow.document.close();
    setTimeout(() => {
        printWindow.focus();
        printWindow.print();
        printWindow.close();
    }, 200);
}