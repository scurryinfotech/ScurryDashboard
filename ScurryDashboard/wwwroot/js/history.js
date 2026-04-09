(function(){
    // Simple history page JS - leverages existing home.js helpers if present
    $(document).ready(function(){
        // Initialize dates to today
        const today = new Date().toISOString().split('T')[0];
        $('#startDate').val(today);
        $('#endDate').val(today);

        $('#btnFilter').on('click', function(){
            loadHistory(true);
        });
        $('#btnClear').on('click', function(){
            $('#startDate').val(today);
            $('#endDate').val(today);
            loadHistory(true);
        });

        loadHistory(true);
    });

    function loadHistory(isInitial){
        $('#orderHistoryTable tbody').html('<tr><td colspan="10" class="text-center p-4 text-muted">Loading…</td></tr>');
        $.ajax({
            url: '/Repository/GetOrderHistory',
            method: 'GET',
            success: function(data){
                window.allOrdersData = data || [];
                // reuse displayOrdersPage from home.js if available
                if (typeof displayOrdersPage === 'function') {
                    // copy data into filteredOrdersData expected by displayOrdersPage
                    window.filteredOrdersData = window.allOrdersData;
                    window.currentPage = 1;
                    window.pageSize = 20;
                    displayOrdersPage(window.filteredOrdersData);
                } else {
                    renderBasicTable(window.allOrdersData);
                }
            },
            error: function(){
                $('#orderHistoryTable tbody').html('<tr><td colspan="10" class="text-center text-danger">Failed to load history</td></tr>');
            }
        });
    }

    function renderBasicTable(data){
        if (!data || data.length === 0){
            $('#orderHistoryTable tbody').html('<tr><td colspan="10" class="text-center text-muted">No history found</td></tr>');
            return;
        }

        const grouped = {};
        data.forEach(o=>{
            const id = o.orderId ?? o.order_id ?? o.id ?? ('ORD_' + Math.random().toString(36).slice(2,6));
            if (!grouped[id]) grouped[id]=[];
            grouped[id].push(o);
        });

        const rows = Object.keys(grouped).map(k=>{
            const g = grouped[k];
            const first = g[0];
            const total = g.reduce((s,it)=>s + (Number(it.amount ?? it.finalAmount ?? it.TotalAmount ?? 0)||0),0);
            const created = first.createdAt ?? first.date ?? first.Date ?? '';
            const dateStr = created ? new Date(created).toLocaleString() : '';
            return `<tr>
                <td><strong>${first.orderId}</strong></td>
                <td>${first.customerName||''}</td>
                <td>${first.phone||''}</td>
                <td>${first.tableNo?('Table '+first.tableNo):'N/A'}</td>
                <td>${first.itemName||''}</td>
                <td>₹${total.toFixed(2)}</td>
                <td>${g.length}</td>
                <td>${first.paymentMode||''}</td>
                <td>${dateStr}</td>
                <td><button class="btn btn-sm btn-info btn-view-bill" data-order-id="${first.orderId}">View Bill</button></td>
            </tr>`;
        });

        $('#orderHistoryTable tbody').html(rows.join(''));
    }

    // Delegate view bill clicks to existing handler in home.js if present
    $(document).on('click', '.btn-view-bill', function(){
        const orderId = $(this).data('order-id');
        if (typeof window.openBillByOrderId === 'function') {
            window.openBillByOrderId(orderId);
            return;
        }
        // fallback to Ajax call that returns bill details
        $.ajax({ url: `/Repository/GetBillData?orderId=${orderId}`, method: 'GET', success: function(bill){
            try {
                // Normalize bill payload to a canonical array of items if necessary
                const billArray = Array.isArray(bill) ? bill : (bill.items ? bill.items : [bill]);

                // Prepare a thermal-friendly global payload so Thermal Print can use it
                const first = billArray[0] || {};
                const thermalItems = billArray.map(item => {
                    const qty = (Number(item.fullPortion) || 0) + (Number(item.halfPortion) || 0) || Number(item.quantity) || 1;
                    const price = Number(item.fullPrice) || Number(item.price) || 0;
                    return { name: item.itemName || item.name || '-', quantity: qty, price: price };
                });

                window.currentBillData = {
                    orderId: first.orderId || first.OrderId || '',
                    customer: first.customerName || first.customer || '',
                    phone: first.phone || '',
                    address: first.address || '',
                    timestamp: first.createdDate || first.createdAt || first.date || new Date(),
                    items: thermalItems,
                    total: thermalItems.reduce((s,it) => s + (it.price * it.quantity), 0),
                    discountAmount: Number(first.discountAmount || first.discount || 0)
                };
            } catch (e) {
                console.warn('Failed to prepare currentBillData', e);
                window.currentBillData = null;
            }

            if (typeof buildBillUI === 'function') {
                buildBillUI(bill);
            } else {
                // simple modal
                $('#bill-content').html('<pre>'+JSON.stringify(bill,null,2)+'</pre>');
                $('#bill-modal').show();
            }
        }});
    });

    $(document).on('click', '#btnThermalPrint', function() {
        // Try existing global payload first
        const b = window.currentBillData;
        const doPrint = (obj) => {
            // Build payload for direct API call
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
                // printThermalBill expects a different "order" shape — conver
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
                // direct API fallback
                fetch('/api/Print/PrintBill', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(apiPayload) })
                    .then(r => r.json()).then(res => showNotification('Thermal print requested', 'success')).catch(err => showNotification('Thermal print failed', 'error'));
            }
        };

        if (b && Array.isArray(b.items) && b.items.length > 0) {
            // If customer/phone missing, try to refresh server data for that order id
            if ((!b.customer || b.customer.trim() === '') || (!b.phone || b.phone.trim() === '')) {
                // attempt to re-fetch full bill details
                const oid = b.orderId;
                if (oid) {
                    fetch(`/Repository/GetBillData?orderId=${encodeURIComponent(oid)}`)
                        .then(r => r.json())
                        .then(bill => {
                            // normalize same as below
                            let obj;
                            if (Array.isArray(bill)) {
                                const first = bill[0] || {};
                                const items = bill.map(it => ({ name: it.itemName || it.name, quantity: (Number(it.fullPortion)||0) + (Number(it.halfPortion)||0) || Number(it.quantity)||1, price: Number(it.fullPrice||it.price||0) }));
                                obj = { orderId: first.orderId || first.OrderId || oid, customer: first.customerName || first.customer || '', phone: first.phone || '', address: first.address || '', timestamp: first.createdDate || first.createdAt || first.date || new Date(), items: items, total: items.reduce((s,i)=>s+(i.price*i.quantity),0), discountAmount: Number(first.discountAmount||first.discount||0) };
                            } else if (bill && bill.items) {
                                obj = { orderId: bill.orderId || oid, customer: bill.customerName || bill.customer || '', phone: bill.phone || '', address: bill.address || '', timestamp: bill.createdDate || bill.createdAt || bill.date || new Date(), items: bill.items.map(it=>({ name: it.itemName||it.name, quantity: it.quantity||1, price: Number(it.price||0) })), total: Number(bill.total||0), discountAmount: Number(bill.discountAmount||bill.discount||0) };
                            } else {
                                const it = bill;
                                const qty = (Number(it.fullPortion)||0) + (Number(it.halfPortion)||0) || Number(it.quantity)||1;
                                obj = { orderId: it.orderId||oid, customer: it.customerName||it.customer||'', phone: it.phone||'', address: it.address||'', timestamp: it.createdDate||it.createdAt||it.date||new Date(), items: [{ name: it.itemName||it.name, quantity: qty, price: Number(it.price||0) }], total: Number(it.amount||it.price||0), discountAmount: Number(it.discountAmount||it.discount||0) };
                            }
                            window.currentBillData = obj;
                            doPrint(obj);
                        })
                        .catch(err => {
                            console.warn('Re-fetch for missing customer/phone failed', err);
                            doPrint(b); // print whatever we have
                        });
                    return;
                }
            }

            doPrint(b);
            return;
        }

        // Fallback: try to parse order id from displayed bill HTML and re-fetch server data
        const billText = $('#bill-content').text() || '';
        const m = billText.match(/Order\s*(?:ID)?\s*[:#\-]?\s*#?([A-Za-z0-9_\-]+)/i);
        const orderId = m ? m[1] : null;
        if (!orderId) {
            alert('No bill selected. Open a bill first.');
            return;
        }

        // Fetch bill data and print
        fetch(`/Repository/GetBillData?orderId=${encodeURIComponent(orderId)}`)
            .then(r => {
                if (!r.ok) throw new Error('Failed to load bill');
                return r.json();
            })
            .then(bill => {
                // normalize into object with items
                let obj;
                if (Array.isArray(bill)) {
                    const first = bill[0] || {};
                    const items = bill.map(it => ({ name: it.itemName || it.name, quantity: (Number(it.fullPortion)||0) + (Number(it.halfPortion)||0) || Number(it.quantity)||1, price: Number(it.fullPrice||it.price||0) }));
                    obj = { orderId: first.orderId || first.OrderId || orderId, customer: first.customerName || first.customer || '', phone: first.phone || '', address: first.address || '', timestamp: first.createdDate || first.createdAt || first.date || new Date(), items: items, total: items.reduce((s,i)=>s+(i.price*i.quantity),0), discountAmount: Number(first.discountAmount||first.discount||0) };
                } else if (bill && bill.items) {
                    obj = { orderId: bill.orderId || orderId, customer: bill.customerName || bill.customer || '', phone: bill.phone || '', address: bill.address || '', timestamp: bill.createdDate || bill.createdAt || bill.date || new Date(), items: bill.items.map(it=>({ name: it.itemName||it.name, quantity: it.quantity||1, price: Number(it.price||0) })), total: Number(bill.total||0), discountAmount: Number(bill.discountAmount||bill.discount||0) };
                } else {
                    // single-row bill
                    const it = bill;
                    const qty = (Number(it.fullPortion)||0) + (Number(it.halfPortion)||0) || Number(it.quantity)||1;
                    obj = { orderId: it.orderId||orderId, customer: it.customerName||it.customer||'', phone: it.phone||'', address: it.address||'', timestamp: it.createdDate||it.createdAt||it.date||new Date(), items: [{ name: it.itemName||it.name, quantity: qty, price: Number(it.price||0) }], total: Number(it.amount||it.price||0), discountAmount: Number(it.discountAmount||it.discount||0) };
                }

                // Store for later and print
                window.currentBillData = obj;
                doPrint(obj);
            })
            .catch(err => {
                console.error('Failed to fetch bill for printing', err);
                alert('Failed to load bill for printing');
            });
    });

})();
