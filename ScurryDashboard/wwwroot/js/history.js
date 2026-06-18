(function () {

    function openBillModal() { $('#bill-modal').removeClass('hidden'); }
    function closeBillModal() { $('#bill-modal').addClass('hidden'); }

    $(document).on('click', '#btnCloseBill, #btnCloseBill2', closeBillModal);

    $(document).on('click', '#bill-modal', function (e) {
        if ($(e.target).is('#bill-modal')) closeBillModal();
    });

    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') closeBillModal();
    });

  
    $(document).ready(function () {
        const today = new Date().toISOString().split('T')[0];
        $('#startDate').val(today);
        $('#endDate').val(today);

        $('#btnFilter').on('click', function () { loadHistory(); });

        $('#btnClear').on('click', function () {
            const todayVal = new Date().toISOString().split('T')[0];
            $('#startDate').val(todayVal);
            $('#endDate').val(todayVal);
            loadHistory();
        });

        loadHistory();
    });

    // ── Revenue summary updater ──────────────────────────────────────
    function updateRevenueSummary(data, startDate, endDate) {
        // Update label
        if (startDate === endDate) {
            const label = startDate === new Date().toISOString().split('T')[0]
                ? "Today's Revenue"
                : "Revenue for " + formatDate(startDate);
            $('#revenueDateRange').text(label);
        } else {
            $('#revenueDateRange').text('Revenue: ' + formatDate(startDate) + ' → ' + formatDate(endDate));
        }

        if (!data || data.length === 0) {
            $('#totalRevenue').text('₹0.00');
            $('#orderCount').text('0 Orders');
            return;
        }

        // Filter by date range
        const start = new Date(startDate);
        start.setHours(0, 0, 0, 0);
        const end = new Date(endDate);
        end.setHours(23, 59, 59, 999);

        const filtered = data.filter(o => {
            const raw = o.createdAt ?? o.date ?? o.Date ?? o.createdDate ?? null;
            if (!raw) return false;
            const d = new Date(raw);
            return d >= start && d <= end;
        });

        // Group by orderId to count unique orders
        const uniqueOrders = {};
        let totalRevenue = 0;

        filtered.forEach(o => {
            const id = o.orderId ?? o.order_id ?? o.id ?? null;
            const amount = Number(o.amount ?? o.finalAmount ?? o.TotalAmount ?? 0) || 0;

            if (id) {
                if (!uniqueOrders[id]) {
                    uniqueOrders[id] = true;
                    // Sum amount once per order (first row has order total)
                }
                totalRevenue += amount;
            } else {
                totalRevenue += amount;
            }
        });

  
        const orderCount = Object.keys(uniqueOrders).length || filtered.length;

        $('#totalRevenue').text('₹' + totalRevenue.toFixed(2));
        $('#orderCount').text(orderCount + (orderCount === 1 ? ' Order' : ' Orders'));
    }

    function formatDate(dateStr) {
        if (!dateStr) return '';
        const d = new Date(dateStr);
        return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
    }

    function loadHistory() {
        const startDate = $('#startDate').val();
        const endDate = $('#endDate').val();

        $('#orderHistoryTable tbody').html(
            '<tr><td colspan="10" class="text-center p-4 text-muted">Loading…</td></tr>'
        );

        $.ajax({
            url: '/Repository/GetOrderHistory',
            method: 'GET',
            success: function (data) {
                window.allOrdersData = data || [];

                updateRevenueSummary(window.allOrdersData, startDate, endDate);

                const start = new Date(startDate); start.setHours(0, 0, 0, 0);
                const end = new Date(endDate); end.setHours(23, 59, 59, 999);

                const filtered = window.allOrdersData.filter(o => {
                    const raw = o.createdAt ?? o.date ?? o.Date ?? o.createdDate ?? null;
                    if (!raw) return false;
                    const d = new Date(raw);
                    return d >= start && d <= end;
                });

                if (typeof displayOrdersPage === 'function') {
                    window.filteredOrdersData = filtered;
                    window.currentPage = 1;
                    window.pageSize = 20;
                    displayOrdersPage(filtered);
                } else {
                    renderBasicTable(filtered);
                    debugger
                }
            },
            error: function () {
                $('#orderHistoryTable tbody').html(
                    '<tr><td colspan="10" class="text-center text-danger">Failed to load history</td></tr>'
                );
                $('#totalRevenue').text('₹0.00');
                $('#orderCount').text('0 Orders');
            }
        });
    }

    // ── Render table ─────────────────────────────────────────────────
    function renderBasicTable(data) {
        if (!data || data.length === 0) {
            $('#orderHistoryTable tbody').html(
                '<tr><td colspan="10" class="text-center text-muted">No history found</td></tr>'
            );
            return;
        }

        const grouped = {};
        data.forEach(o => {
            const id = o.orderId ?? o.order_id ?? o.id ?? ('ORD_' + Math.random().toString(36).slice(2, 6));
            if (!grouped[id]) grouped[id] = [];
            grouped[id].push(o);
        });

        const rows = Object.keys(grouped).map(k => {
            const g = grouped[k];
            const first = g[0];
            const total = g.reduce((s, it) => s + (Number(it.amount ?? it.finalAmount ?? it.TotalAmount ?? 0) || 0), 0);
            const created = first.createdAt ?? first.date ?? first.Date ?? first.createdDate ?? '';
            const dateStr = created ? new Date(created).toLocaleString('en-IN') : '';
            return `<tr>
                <td><strong>${first.orderId}</strong></td>
                <td>${first.customerName || ''}</td>
                <td>${first.phone || ''}</td>
                <td>${first.tableNo ? ('Table ' + first.tableNo) : 'N/A'}</td>
                <td>${first.itemName || ''}</td>
                <td>₹${total.toFixed(2)}</td>
                <td>${g.length}</td>
                <td>${first.paymentMode || ''}</td>
                <td>${dateStr}</td>
                <td><button class="btn btn-sm btn-info btn-view-bill" data-order-id="${first.orderId}">View Bill</button></td>
            </tr>`;
        });

        $('#orderHistoryTable tbody').html(rows.join(''));
    }

    // ── View Bill button ─────────────────────────────────────────────
    $(document).on('click', '.btn-view-bill', function () {
        const orderId = $(this).data('order-id');

        if (typeof window.openBillByOrderId === 'function') {
            window.openBillByOrderId(orderId);
            return;
        }

        $.ajax({
            url: `/Repository/GetBillData?orderId=${orderId}`,
            method: 'GET',
            success: function (bill) {
                try {
                    const billArray = Array.isArray(bill) ? bill : (bill.items ? bill.items : [bill]);
                    const first = billArray[0] || {};
                    const thermalItems = billArray.map(item => {
                        const qty = (Number(item.fullPortion) || 0) + (Number(item.halfPortion) || 0) || Number(item.quantity) || 1;
                        const price = Number(item.fullPrice) || Number(item.price) || 0;
                        return { name: item.itemName || item.name || '-', quantity: qty, price };
                    });

                    window.currentBillData = {
                        orderId: first.orderId || first.OrderId || '',
                        customer: first.customerName || first.customer || '',
                        phone: first.phone || '',
                        address: first.address || '',
                        timestamp: first.createdDate || first.createdAt || first.date || new Date(),
                        items: thermalItems,
                        total: thermalItems.reduce((s, it) => s + (it.price * it.quantity), 0),
                        discountAmount: Number(first.discountAmount || first.discount || 0)
                    };
                } catch (e) {
                    console.warn('Failed to prepare currentBillData', e);
                    window.currentBillData = null;
                }

                if (typeof buildBillUI === 'function') {
                    buildBillUI(bill);
                } else {
                    $('#bill-content').html('<pre>' + JSON.stringify(bill, null, 2) + '</pre>');
                }

                openBillModal();
            }
        });
    });

    // ── Thermal Print button ─────────────────────────────────────────
    $(document).on('click', '#btnThermalPrint', function () {
        const b = window.currentBillData;

        const doPrint = (obj) => {
            const apiPayload = {
                OrderId: obj.orderId || '',
                Name: obj.customer || '',
                Phone: obj.phone || '',
                Address: obj.address || '',
                OrderTime: obj.timestamp ? new Date(obj.timestamp) : new Date(),
                Items: (obj.items || []).map(it => ({ Name: it.name || it.itemName || '-', Quantity: it.quantity || it.qty || 1, Price: Number(it.price || it.Price || 0) })),
                Subtotal: Number(obj.total) || 0,
                Discount: Number(obj.discountAmount || obj.discount || 0) || 0,
                Total: Number(obj.total) || 0,
                PrinterName: 'Everycom-58-Series'
            };

            if (typeof printThermalBill === 'function') {
                const orderLike = {
                    orderId: apiPayload.OrderId,
                    customer: apiPayload.Name,
                    phone: apiPayload.Phone,
                    address: apiPayload.Address,
                    timestamp: apiPayload.OrderTime,
                    items: (apiPayload.Items || []).map(i => ({ name: i.Name, quantity: i.Quantity, price: i.Price })),
                    total: apiPayload.Total,
                    discountAmount: apiPayload.Discount
                };
                printThermalBill(orderLike);
                showNotification('Thermal print requested', 'success');
            } else {
                fetch('/api/Print/PrintBill', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(apiPayload)
                })
                    .then(r => r.json())
                    .then(() => showNotification('Thermal print requested', 'success'))
                    .catch(() => showNotification('Thermal print failed', 'error'));
            }
        };

        const normalizeBill = (bill, fallbackOid) => {
            if (Array.isArray(bill)) {
                const first = bill[0] || {};
                const items = bill.map(it => ({ name: it.itemName || it.name, quantity: (Number(it.fullPortion) || 0) + (Number(it.halfPortion) || 0) || Number(it.quantity) || 1, price: Number(it.fullPrice || it.price || 0) }));
                return { orderId: first.orderId || first.OrderId || fallbackOid, customer: first.customerName || first.customer || '', phone: first.phone || '', address: first.address || '', timestamp: first.createdDate || first.createdAt || first.date || new Date(), items, total: items.reduce((s, i) => s + (i.price * i.quantity), 0), discountAmount: Number(first.discountAmount || first.discount || 0) };
            } else if (bill && bill.items) {
                return { orderId: bill.orderId || fallbackOid, customer: bill.customerName || bill.customer || '', phone: bill.phone || '', address: bill.address || '', timestamp: bill.createdDate || bill.createdAt || bill.date || new Date(), items: bill.items.map(it => ({ name: it.itemName || it.name, quantity: it.quantity || 1, price: Number(it.price || 0) })), total: Number(bill.total || 0), discountAmount: Number(bill.discountAmount || bill.discount || 0) };
            } else {
                const it = bill;
                const qty = (Number(it.fullPortion) || 0) + (Number(it.halfPortion) || 0) || Number(it.quantity) || 1;
                return { orderId: it.orderId || fallbackOid, customer: it.customerName || it.customer || '', phone: it.phone || '', address: it.address || '', timestamp: it.createdDate || it.createdAt || it.date || new Date(), items: [{ name: it.itemName || it.name, quantity: qty, price: Number(it.price || 0) }], total: Number(it.amount || it.price || 0), discountAmount: Number(it.discountAmount || it.discount || 0) };
            }
        };

        const refetchAndPrint = (oid) => {
            fetch(`/Repository/GetBillData?orderId=${encodeURIComponent(oid)}`)
                .then(r => r.json())
                .then(bill => { const obj = normalizeBill(bill, oid); window.currentBillData = obj; doPrint(obj); })
                .catch(err => { console.warn('Re-fetch failed', err); if (b) doPrint(b); else alert('Failed to load bill for printing'); });
        };

        if (b && Array.isArray(b.items) && b.items.length > 0) {
            const missingInfo = (!b.customer || b.customer.trim() === '') || (!b.phone || b.phone.trim() === '');
            if (missingInfo && b.orderId) { refetchAndPrint(b.orderId); return; }
            doPrint(b);
            return;
        }

        const billText = $('#bill-content').text() || '';
        const m = billText.match(/Order\s*(?:ID)?\s*[:#\-]?\s*#?([A-Za-z0-9_\-]+)/i);
        const orderId = m ? m[1] : null;
        if (!orderId) { alert('No bill selected. Open a bill first.'); return; }
        refetchAndPrint(orderId);
    });

})();