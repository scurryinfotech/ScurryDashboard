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
        // fallback to Ajax call that home.js GetBillData supports
        $.ajax({ url: `/Repository/GetBillData?orderId=${orderId}`, method: 'GET', success: function(bill){
            if (typeof buildBillUI === 'function') {
                buildBillUI(bill);
            } else {
                // simple modal
                $('#bill-content').html('<pre>'+JSON.stringify(bill,null,2)+'</pre>');
                $('#bill-modal').show();
            }
        }});
    });

})();
