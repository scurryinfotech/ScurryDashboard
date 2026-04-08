

/* ── API Endpoints ── */
var API = {
    cats: '/Repository/GetMenuCategory',
    subs: '/Repository/GetMenuSubcategory',
    items: '/Repository/GetMenuItem',
    save: '/Repository/SaveTableOrder',
    tables: '/Repository/GetTableCount',
};

// Use server-provided user identifier when available to ensure orders originate from dashboard
const APP_USER = (window && window.__APP_USER__) ? window.__APP_USER__ : 2;

var TAX = 0.05;

/* ── State ── */
var _C = [];   // [{ id, name }]
var _SC = [];   // [{ id, catId, name }]
var _I = [];   // [{ id, name, price1, price2, subCatId, img, avail }]
var _MSC = {};   // catId  → [subcategory, ...]
var _MI = {};   // scId   → [item, ...]
var _cart = {};   // itemId → { fq, hq, note, name, fp, hp }

var _activeCat = null;
var _activeSC = null;
// Default to Table so dashboard opens in table mode by default
var _otype = 'Table';
var _dmode = 'none', _dpct = 0, _damt = 0;
var _pm = 'Cash';

/* Detail panel */
var _dItem = null;
var _dPortion = 'full';
var _dQty = 1;

/* ════════════════════════════════════════════════════════════
   DOCUMENT READY — jQuery entry point
════════════════════════════════════════════════════════════ */
$(document).ready(function () {

    _initSkeletons();

    /* Load all 3 APIs in parallel, then tables */
    $.when(
        _ajaxGet(API.cats),
        _ajaxGet(API.subs),
        _ajaxGet(API.items)
    ).done(function (cRes, sRes, iRes) {

        var rawCats = cRes[0];
        var rawSubs = sRes[0];
        var rawItems = iRes[0];

        /* ── Map Categories ── */
        _C = $.map(rawCats, function (c) {
            return { id: c.categoryId, name: c.categoryName || 'Category' };
        });

        /* ── Map Subcategories ── */
        _SC = $.map(rawSubs, function (s) {
            return { id: s.subcategoryId, catId: s.categoryId, name: s.subcategoryName || 'Sub' };
        });

        /* ── Map Items ──
             price1 = full price
             price2 = half price (0 means no half available)
             imageSrc = base64 image string
        ── */
        _I = $.map(rawItems, function (i) {
            if (!i.isActive) return null; // skip inactive
            return {
                id: i.itemId,
                name: i.itemName || '',
                fp: parseFloat(i.price1) || 0,
                hp: parseFloat(i.price2) || 0,
                subCatId: i.subcategoryId,
                img: i.imageSrc || '',
                avail: i.isActive !== false,
            };
        });

        /* ── Build lookup maps ── */
        _MSC = {};
        $.each(_C, function (_, c) {
            _MSC[c.id] = $.grep(_SC, function (s) {
                return String(s.catId) === String(c.id);
            });
        });

        _MI = {};
        $.each(_SC, function (_, sc) {
            _MI[sc.id] = $.grep(_I, function (i) {
                return String(i.subCatId) === String(sc.id);
            });
        });

        /* ── Set first active category ── */
        if (_C.length) {
            _activeCat = _C[0].id;
            var firstSubs = _MSC[_activeCat] || [];
            _activeSC = firstSubs.length ? firstSubs[0].id : null;
        }

        renderCats();
        renderSubcats();
        renderGrid();
        renderCart();

    }).fail(function () {
        console.error('[GNS] API load failed — using demo data');
        _demoData();
        renderCats(); renderSubcats(); renderGrid(); renderCart();
    });

    /* Load table list separately */
    _loadTables();

    // If opened with ?table= query param, remember it so _loadTables can set selection
    try {
        var urlParams = new URLSearchParams(window.location.search);
        window.__initialTableParam = urlParams.get('table');
        // If provided, ensure NewOrder locks the table selection and switches to Table order type
        if (window.__initialTableParam) {
            try { $('#noTableSel').val(parseInt(window.__initialTableParam)); } catch (e) { }
            // Force order type to Table
            _otype = 'Table';
            $('.no-type-tab').removeClass('active');
            $('.no-type-tab[data-ot="Table"]').addClass('active');
            // Disable manual change of table selection
            $(document).ready(function () {
                $('#noTableSel').prop('disabled', true);
            });
        }
    } catch (e) { window.__initialTableParam = null; }

    /* Keyboard close */
    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') {
            if ($('#noDetailPanel').hasClass('open')) { closeDetail(); return; }
            $('#noKOTModal, #noBillModal').removeClass('open');
        }
    });

    /* Modal backdrop close */
    $('#noKOTModal, #noBillModal').on('click', function (e) {
        if (e.target === this) $(this).removeClass('open');
    });
});

/* ════════════════════════════════════════════════════════════
   AJAX HELPER — returns jQuery deferred
════════════════════════════════════════════════════════════ */
function _ajaxGet(url) {
    return $.ajax({
        url: url,
        type: 'GET',
        dataType: 'json',
        headers: { 'Cache-Control': 'no-cache' },
    });
}

/* ════════════════════════════════════════════════════════════
   LOAD TABLE LIST
════════════════════════════════════════════════════════════ */
function _loadTables() {
    $.ajax({
        url: API.tables,
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var $sel = $('#noTableSel').empty();

            // Handle plain number response (e.g., 5) from GetTableCount
            if ($.isArray(data) && data.length) {
                $.each(data, function (_, t) {
                    var no = t.tableNumber || t.table_number || t.tableNo || t.table_no || t;
                    $sel.append($('<option>').val(no).text('Table ' + no));
                });
                return;
            }

            var cnt = 0;
            if (typeof data === 'number') {
                cnt = data;
            } else if (data && typeof data === 'object') {
                cnt = data.count || data.tableCount || data.TableCount || 0;
            }
            if (cnt <= 0) cnt = 20; // safe fallback

            for (var i = 1; i <= cnt; i++) {
                $sel.append($('<option>').val(i).text('Table ' + i));
            }

            // If initial table specified in query, set it
            try {
                if (window.__initialTableParam) {
                    var tval = parseInt(window.__initialTableParam);
                    if (!isNaN(tval)) $sel.val(tval);
                }
            } catch (e) { }
        },
        error: function () {
            var $sel = $('#noTableSel').empty();
            for (var i = 1; i <= 20; i++) {
                $sel.append($('<option>').val(i).text('Table ' + i));
            }
            try {
                if (window.__initialTableParam) $('#noTableSel').val(parseInt(window.__initialTableParam));
            } catch (e) { }
        }
    });
}

function _initSkeletons() {
    var cats = '';
    for (var i = 0; i < 8; i++)  cats += '<div class="no-cat-skel"></div>';
    $('#noCatBar').html(cats);

    var grid = '';
    for (var i = 0; i < 16; i++) grid += '<div class="no-skel"></div>';
    $('#noGrid').html(grid);
}


function renderCats() {
    var html = '';
    $.each(_C, function (_, c) {
        var active = String(c.id) === String(_activeCat) ? ' active' : '';
        html += '<div class="no-cat-item' + active + '" onclick="selCat(\'' + c.id + '\',this)">' +
            c.name.replace(/_/g, ' ') + '</div>';
    });
    $('#noCatBar').html(html || '<div style="padding:14px;font-size:.72rem;color:#aaa">No categories</div>');
}

function selCat(id, el) {
    _activeCat = id;
    $('.no-cat-item').removeClass('active');
    $(el).addClass('active');
    var firstSubs = _MSC[id] || [];
    _activeSC = firstSubs.length ? firstSubs[0].id : null;
    clearSearch();
    renderSubcats();
    renderGrid();
    closeDetail();
}

function renderSubcats() {
    var subs = _MSC[_activeCat] || [];
    var $bar = $('#noSubBar');
    if (!subs.length) { $bar.hide(); return; }
    $bar.show();
    var html = '';
    $.each(subs, function (_, sc) {
        var active = String(sc.id) === String(_activeSC) ? ' active' : '';
        html += '<div class="no-sub-tab' + active + '" onclick="selSub(\'' + sc.id + '\',this)">' +
            sc.name + '</div>';
    });
    $bar.html(html);
}

function selSub(id, el) {
    _activeSC = id;
    $('.no-sub-tab').removeClass('active');
    $(el).addClass('active');
    clearSearch();
    renderGrid();
    closeDetail();
}


function renderGrid() {
    var q = $('#noSearch').val().toLowerCase().trim();
    var sc = $('#noSC').val().toLowerCase().trim();

    var items;
    if (q || sc) {
        /* Search across ALL items */
        items = $.grep(_I, function (i) {
            return (!q || i.name.toLowerCase().indexOf(q) >= 0) &&
                (!sc || i.name.toLowerCase().indexOf(sc) >= 0);
        });
    } else {
        items = _activeSC ? (_MI[_activeSC] || []) : [];
    }

    var $g = $('#noGrid');
    if (!items.length) {
        $g.html('<div class="no-noitems"><i class="fas fa-bowl-food"></i><p>No items found</p></div>');
        return;
    }

    var html = '';
    $.each(items, function (_, item) {
        var e = _cart[item.id];
        var qty = e ? (e.fq + e.hq) : 0;
        var hasH = item.hp > 0;
        var inCart = qty > 0 ? ' in-cart' : '';
        var unavail = !item.avail ? ' unavail' : '';

        /* Build image tag if imageSrc exists */
        var imgTag = '';
        if (item.img) {
            /* Detect format: WEBP starts with RIFF, JPEG with /9j, PNG with iVBOR */
            var mime = 'image/jpeg';
            if (item.img.indexOf('RIFF') === 0 || item.img.indexOf('UklG') === 0) mime = 'image/webp';
            else if (item.img.indexOf('iVBOR') === 0) mime = 'image/png';
            imgTag = '<img class="no-iimg" src="data:' + mime + ';base64,' + item.img + '" alt="' + item.name + '" />';
        }

        /* Qty controls */
        var qtyHtml = '';
        if (qty > 0) {
            qtyHtml = '<button class="no-iqb" onclick="event.stopPropagation();quickMinus(' + item.id + ')">&#8722;</button>' +
                '<span class="no-iqv">' + qty + '</span>' +
                '<button class="no-iqb" onclick="event.stopPropagation();showDetail(' + item.id + ')">+</button>';
        } else {
            qtyHtml = '<button class="no-iqb add-btn" onclick="event.stopPropagation();showDetail(' + item.id + ')"><i class="fas fa-plus"></i> Add</button>';
        }

        html += '<div class="no-item' + inCart + unavail + '" data-id="' + item.id + '">' +
            (qty > 0 ? '<div class="no-cbadge">' + qty + '</div>' : '') +
            imgTag +
            '<div class="no-vd veg"></div>' +
            '<div class="no-iname">' + item.name + '</div>' +
            '<div class="no-iprices">' +
            '<span class="no-iprice">&#8377;' + item.fp.toFixed(2) + '</span>' +
            (hasH ? '<span class="no-ihalf">H: &#8377;' + item.hp.toFixed(2) + '</span>' : '') +
            '</div>' +
            '<div class="no-iqty">' + qtyHtml + '</div>' +
            '</div>';
    });
    $g.html(html);

    /* Card click = open detail */
    $('#noGrid .no-item').on('click', function (e) {
        if ($(e.target).is('button') || $(e.target).is('i')) return;
        showDetail($(this).data('id'));
    });
}

function filterMenu() { renderGrid(); }
function clearSearch() { $('#noSearch, #noSC').val(''); }

/* ════════════════════════════════════════════════════════════
   ITEM DETAIL PANEL
════════════════════════════════════════════════════════════ */
function showDetail(id) {
    var item = null;
    $.each(_I, function (_, i) { if (String(i.id) === String(id)) { item = i; return false; } });
    if (!item) return;

    _dItem = item;
    _dPortion = 'full';
    _dQty = 1;
    var hasH = item.hp > 0;

    $('#noDetailName').text(item.name);
    $('#noDetailCode').text('');
    $('#noDetailFP').html('&#8377;' + item.fp.toFixed(2));
    $('#noDetailHP').html(hasH ? '&#8377;' + item.hp.toFixed(2) : 'N/A');
    $('#noDetailQty').text('1');
    $('#noDetailNote').val('');
    $('#noPortionHalf').toggleClass('na', !hasH);

    hlPortion('full');
    $('#noDetailPanel').addClass('open');
}

function hlPortion(p) {
    _dPortion = p;
    $('#noPortionFull').toggleClass('active', p === 'full');
    $('#noPortionHalf').toggleClass('active', p === 'half');
}
function selPortion(p) {
    if (p === 'half' && _dItem && !_dItem.hp) return;
    hlPortion(p);
}

function detailChgQty(d) {
    _dQty = Math.max(1, _dQty + d);
    $('#noDetailQty').text(_dQty);
}

function detailAdd() {
    if (!_dItem) return;
    var id = _dItem.id;
    var note = $('#noDetailNote').val();
    if (!_cart[id]) {
        _cart[id] = { fq: 0, hq: 0, note: '', name: _dItem.name, fp: _dItem.fp, hp: _dItem.hp };
    }
    if (note) _cart[id].note = note;
    if (_dPortion === 'half') _cart[id].hq += _dQty;
    else _cart[id].fq += _dQty;
    closeDetail();
    renderGrid();
    renderCart();
    //toast(_dItem.name + ' added!', 'success');
}

function closeDetail() {
    $('#noDetailPanel').removeClass('open');
    _dItem = null;
}

function quickMinus(id) {
    if (!_cart[id]) return;
    if (_cart[id].fq > 0) _cart[id].fq--;
    else if (_cart[id].hq > 0) _cart[id].hq--;
    if (_cart[id].fq === 0 && _cart[id].hq === 0) delete _cart[id];
    renderGrid();
    renderCart();
}

/* ════════════════════════════════════════════════════════════
   ORDER TYPE
════════════════════════════════════════════════════════════ */
function setType(el) {
    _otype = $(el).data('ot');
    $('.no-type-tab').removeClass('active');
    $(el).addClass('active');
    $('#noCustAddr').toggleClass('show', _otype === 'Delivery');
    $('#noTblWrap').toggleClass('show', _otype === 'Table');
}

/* ════════════════════════════════════════════════════════════
   CART RENDER
════════════════════════════════════════════════════════════ */
function renderCart() {
    var entries = Object.entries(_cart);
    var $list = $('#noCartList');

    if (!entries.length) {
        $list.html(
            '<div class="no-empty">' +
            '<i class="fas fa-cart-shopping"></i>' +
            '<p>No items selected</p></div>'
        );
    } else {
        var html = '';
        $.each(entries, function (_, pair) {
            var id = pair[0], e = pair[1];
            var total = (e.fq * e.fp) + (e.hq * (e.hp || e.fp / 2));

            // Show full and half portions separately to avoid confusion
            html += '<div class="no-crow">' +
                '<div class="no-crow-name">' + e.name +
                (e.note ? '<div class="no-crow-code" title="' + e.note + '">' + e.note + '</div>' : '') +
                '</div>' +
                '<div class="no-crow-qty">' +
                '<div class="no-portion-row">' +
                    '<span class="no-portion-label">F:</span>' +
                    '<button class="no-cqb" onclick="cartChg(' + id + ',\'fq\',-1)">&#8722;</button>' +
                    '<span class="no-iqv">' + e.fq + '</span>' +
                    '<button class="no-cqb" onclick="cartAdd(' + id + ',\'fq\')">+</button>' +
                '</div>' +
                '<div class="no-portion-row">' +
                    '<span class="no-portion-label">H:</span>' +
                    '<button class="no-cqb" onclick="cartChg(' + id + ',\'hq\',-1)">&#8722;</button>' +
                    '<span class="no-iqv">' + e.hq + '</span>' +
                    '<button class="no-cqb" onclick="cartAdd(' + id + ',\'hq\')">+</button>' +
                '</div>' +
                '</div>' +
                '<div class="no-crow-price">&#8377;' + total.toFixed(2) + '</div>' +
                '<button class="no-cdel" onclick="cartDel(' + id + ')"><i class="fas fa-xmark"></i></button>' +
                '</div>';
        });
        $list.html(html);
    }

    /* Update meta */
    var totalQty = 0;
    var totalItems = entries.length;
    $.each(entries, function (_, p) { totalQty += p[1].fq + p[1].hq; });

    /* Update billing */
    billing();

    /* Enable/disable buttons */
    var has = totalItems > 0;
    $('#noBtnSave, #noBtnPrint, #noBtnEB, #noBtnKOT, #noBtnKOTP, #noBtnHold, #noBtnPlace')
        .prop('disabled', !has);
}

function cartChg(id, type, d) {
    if (!_cart[id]) return;
    _cart[id][type] = Math.max(0, _cart[id][type] + d);
    if (_cart[id].fq === 0 && _cart[id].hq === 0) delete _cart[id];
    renderGrid();
    renderCart();
}
function cartAdd(id) {
    // support adding either full or half via second arg
    var portion = 'fq';
    if (arguments.length > 1 && (arguments[1] === 'hq' || arguments[1] === 'fq')) portion = arguments[1];
    if (!_cart[id]) return;
    _cart[id][portion]++;
    renderGrid();
    renderCart();
}
function cartDel(id) {
    delete _cart[id];
    renderGrid();
    renderCart();
}
function clearCart() {
    _cart = {};
    _dmode = 'none'; _dpct = 0; _damt = 0;
    $('#noDiscAmt').val('');
    $('#noComp').prop('checked', false);
    $('.no-dpct').removeClass('active');
    renderGrid();
    renderCart();
}

/* ════════════════════════════════════════════════════════════
   BILLING
════════════════════════════════════════════════════════════ */
function _sub() {
    var total = 0;
    $.each(_cart, function (_, e) {
        total += (e.fq * e.fp) + (e.hq * (e.hp || e.fp / 2));
    });
    return total;
}

function billing() {
    var sub = _sub(), disc = 0;
    if (_dmode === 'comp') disc = sub;
    else if (_dmode === 'pct') disc = sub * _dpct / 100;
    else if (_dmode === 'amt') disc = Math.min(_damt, sub);
    var after = sub - disc,
        tax = after * TAX,
        raw = after + tax,
        grand = Math.round(raw),
        round = grand - raw;

    $('#noBSub').text(sub.toFixed(2));
    $('#noBDisc').text('(' + disc.toFixed(2) + ')');
    $('#noBTax').text(tax.toFixed(2));
    $('#noBRound').text(round.toFixed(2));
    $('#noTotal').text(grand);
    updateRet(grand);
}

function updateRet(grand) {
    if (!grand) grand = parseInt($('#noTotal').text()) || 0;
    var paid = parseFloat($('#noPaid').val()) || 0;
    $('#noBRet').text(Math.max(0, paid - grand).toFixed(2));
}

function discPct(pct) {
    if (_dmode === 'pct' && _dpct === pct) {
        _dmode = 'none'; _dpct = 0;
        $('.no-dpct').removeClass('active');
    } else {
        _dmode = 'pct'; _dpct = pct; _damt = 0;
        $('#noDiscAmt').val('');
        $('.no-dpct').removeClass('active');
        $('[data-pct="' + pct + '"]').addClass('active');
    }
    billing();
}
function discFixed(val) {
    _damt = parseFloat(val) || 0;
    _dmode = _damt > 0 ? 'amt' : 'none';
    _dpct = 0;
    $('.no-dpct').removeClass('active');
    billing();
}
function toggleComp() {
    _dmode = $('#noComp').is(':checked') ? 'comp' : 'none';
    _dpct = 0; _damt = 0;
    $('#noDiscAmt').val('');
    $('.no-dpct').removeClass('active');
    billing();
}
function selectPM(el) {
    $('.no-pm-tab').removeClass('active');
    $(el).addClass('active');
    _pm = $(el).data('mode');
}


function snap() {
    var sub = _sub(), disc = 0;
    if (_dmode === 'comp') disc = sub;
    else if (_dmode === 'pct') disc = sub * _dpct / 100;
    else if (_dmode === 'amt') disc = Math.min(_damt, sub);
    var after = sub - disc, tax = after * TAX, grand = Math.round(after + tax);

    // Only set tableNo when type is 'Table' and a valid table is selected
    var tno = _otype === 'Table' ? (parseInt($('#noTableSel').val()) || null) : null;

    var lines = [];
    $.each(_cart, function (id, e) {
        lines.push({
            itemId: parseInt(id),
            itemName: e.name,
            fullPortion: e.fq,
            halfPortion: e.hq,
            unitFullPrice: e.fp,                        
            unitHalfPrice: e.hp || Math.round(e.fp / 2), 
            specialNote: e.note || '',
        });
    });
    return {
        lines: lines,
        subtotal: sub,
        discount: disc,
        tax: tax,
        grandTotal: grand,
        paid: parseFloat($('#noPaid').val()) || 0,
        payMode: _pm,
        orderType: _otype,
        tableNo: tno,
        customerName: $('#noCustName').val().trim(),
        phone: $('#noCustPhone').val().trim(),
        address: $('#noCustAddr').val().trim(),
    };
}

function validate() {
    $('#noCustName, #noCustPhone').removeClass('err');

    if (_otype === 'Table') {
        var tableVal = parseInt($('#noTableSel').val());
        if (!tableVal || tableVal <= 0) {
            toast('Please select a valid table number', 'error');
            return false;
        }
        return true;
    }

    var ok = true;
    if (!$('#noCustName').val().trim()) { $('#noCustName').addClass('err'); ok = false; }
    if (!$('#noCustPhone').val().trim()) { $('#noCustPhone').addClass('err'); ok = false; }
    if (!ok) toast('Customer name & phone are required', 'error');
    return ok;
}


function noKOT(andPrint) {
    if (!Object.keys(_cart).length) return;
    var s = snap();
    $('#noKOTPrint').html(buildKOT(s));
    $('#noKOTModal').addClass('open');
    if (andPrint) setTimeout(function () { printZone('noKOTPrint'); }, 300);
}

function noSave(andPrint) {
    if (!Object.keys(_cart).length) return;
    if (!validate()) return;
    var s = snap();
    $('#noBillPrint').html(buildBill(s));
    $('#noBillModal').addClass('open');
    if (andPrint) setTimeout(function () { printZone('noBillPrint'); }, 300);
    postOrder(s);

    // If opened from Table modal via query param, close and return to table view
    try {
        if (window.__initialTableParam) {
            // After successful post (server response), navigate back to home table service
            setTimeout(function () { window.location.href = '/Home/Index'; }, 600);
        }
    } catch (e) { }
}

function noPlace() {
    if (!Object.keys(_cart).length) return;
    if (!validate()) return;
    postOrder(snap());
    toast('Order placed! &#10003;', 'success');
    setTimeout(clearCart, 1400);
}

function noHold() { toast('Order on hold', 'info'); }

function doneBill(mid) {
    $('#' + mid).removeClass('open');
    clearCart();
    toast('Order saved! &#10003;', 'success');
}


function postOrder(s) {
    var notes = $.map(s.lines, function (l) {
        return l.specialNote ? l.itemName + ': ' + l.specialNote : null;
    }).join('; ');

    // Map order type to lowercase DB value
    var orderType = s.orderType === 'DineIn' ? 'dine'
        : s.orderType === 'Delivery' ? 'delivery'
            : s.orderType === 'PickUp' ? 'pickup'
                : s.orderType === 'Table' ? 'dine'  
                    : 'dine';

    var orderItems = [];
    $.each(s.lines, function (_, l) {
        if (l.fullPortion > 0) {
            orderItems.push({
                item_id: l.itemId,
                full: l.fullPortion,
                half: 0,
                Price: l.unitFullPrice   
            });
        }
        if (l.halfPortion > 0) {
            orderItems.push({
                item_id: l.itemId,
                full: 0,
                half: l.halfPortion,
                Price: l.unitHalfPrice  
            });
        }
    });

    var payload = {
        selectedTable: s.tableNo ? parseInt(s.tableNo) : null,
        userName: APP_USER,
        customerName: s.customerName || '',
        userPhone: s.phone || '',
        Address: s.address || null,
        OrderType: orderType,
        paymentMode: s.payMode || 'Cash',
        specialInstruction: notes || 'No Instructions',
        orderItems: orderItems
    };

    $.ajax({
        url: API.save, 
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function () {
            console.log('[GNS] Order saved OK — table:', s.tableNo, 'type:', orderType);
            // If this order was created for a table via query param, close the NewOrder page and go back
            try {
                if (window.__initialTableParam) {
                    // Give a short delay for user feedback then navigate back to home (table service)
                    setTimeout(function () { window.location.href = '/Home/Index'; }, 300);
                } else {
                    // Otherwise, if running as a modal in same page, refresh table orders
                    if (typeof loadTableOrders === 'function') loadTableOrders();
                    if (typeof loadTableCount === 'function') loadTableCount();
                }
            } catch (e) { }
        },
        error: function (xhr, status, err) {
            console.error('[GNS] Post error:', xhr.status, err, xhr.responseText);
            if (xhr.status === 401)
                toast('Session expired — please log in again', 'error');
            else
                toast('Save failed — please try again', 'error');
        }
    });
}


function _orderTypeBadge(otype, tno) {
    if (otype === 'Delivery') return '&#128757; DELIVERY';
    if (otype === 'PickUp') return '&#128717; PICK UP';
    if (otype === 'Table') return '&#128196; TABLE ' + (tno || '');
    return '&#128722; DINE IN';
}

function buildKOT(s) {
    var now = new Date();
    var badge = _orderTypeBadge(s.orderType, s.tableNo);
    var lines = '';
    $.each(s.lines, function (_, l) {
        var qty = (l.fullPortion > 0 ? l.fullPortion + 'F ' : '') +
            (l.halfPortion > 0 ? l.halfPortion + 'H' : '');
        var note = l.specialNote ? '<br><em style="font-size:.6rem;color:#888">' + l.specialNote + '</em>' : '';
        lines += '<div class="ri"><span class="riq">' + $.trim(qty) + '</span>' +
            '<span class="ril">' + l.itemName + note + '</span></div>';
    });
    return '<div class="no-rcpt">' +
        '<div class="rh"><h2>GRILL N SHAKES</h2><p>Kitchen Order Ticket</p>' +
        '<span class="rbadge">' + badge + '</span>' +
        '<p style="margin-top:4px">' +
        now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) +
        ' &middot; ' + now.toLocaleDateString() + '</p>' +
        (s.customerName ? '<p><strong>' + s.customerName + '</strong>' + (s.phone ? ' &middot; ' + s.phone : '') + '</p>' : '') +
        (s.address ? '<p style="font-size:.62rem">' + s.address + '</p>' : '') +
        '</div>' + lines +
        '<div class="rftr">*** KOT — NOT A BILL ***</div></div>';
}

function buildBill(s) {
    var now = new Date();
    var badge = _orderTypeBadge(s.orderType, s.tableNo);
    var ret = Math.max(0, s.paid - s.grandTotal);
    var lines = '';
    $.each(s.lines, function (_, l) {
        var qty = (l.fullPortion > 0 ? l.fullPortion + 'F ' : '') +
            (l.halfPortion > 0 ? l.halfPortion + 'H' : '');
        var note = l.specialNote ? '<br><em style="font-size:.6rem;color:#888">' + l.specialNote + '</em>' : '';
        lines += '<div class="ri">' +
            '<span class="riq">' + $.trim(qty) + '</span>' +
            '<span class="ril">' + l.itemName + note + '</span>' +
            '<strong>&#8377;' + l.price.toFixed(2) + '</strong></div>';
    });
    return '<div class="no-rcpt">' +
        '<div class="rh"><h2>GRILL N SHAKES</h2><p>The Finest Restaurant Experience</p>' +
        '<span class="rbadge">' + badge + '</span></div>' +
        '<div class="rmeta">' +
        (s.customerName ? '<div class="rmr"><span>Customer</span><strong>' + s.customerName + '</strong></div>' : '') +
        (s.phone ? '<div class="rmr"><span>Phone</span><span>' + s.phone + '</span></div>' : '') +
        (s.address ? '<div class="rmr"><span>Address</span><span>' + s.address + '</span></div>' : '') +
        (s.tableNo ? '<div class="rmr"><span>Table</span><span>' + s.tableNo + '</span></div>' : '') +
        '<div class="rmr"><span>Date</span><span>' + now.toLocaleDateString() + '</span></div>' +
        '<div class="rmr"><span>Time</span><span>' + now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '</span></div>' +
        '<div class="rmr"><span>Payment</span><span>' + s.payMode + '</span></div></div>' +
        lines +
        '<div class="rtot">' +
        '<div class="rtr"><span>Subtotal</span><span>&#8377;' + s.subtotal.toFixed(2) + '</span></div>' +
        (s.discount > 0 ? '<div class="rtr"><span>Discount</span><span style="color:#c0392b">-&#8377;' + s.discount.toFixed(2) + '</span></div>' : '') +
        '<div class="rtr"><span>Tax (5%)</span><span>&#8377;' + s.tax.toFixed(2) + '</span></div>' +
        '<div class="rtr grand"><span>TOTAL</span><span>&#8377;' + s.grandTotal + '</span></div>' +
        (s.paid > 0
            ? '<div class="rtr"><span>Paid</span><span>&#8377;' + s.paid.toFixed(2) + '</span></div>' +
            '<div class="rtr"><span style="color:#27ae60">Return</span>' +
            '<span style="color:#27ae60">&#8377;' + ret.toFixed(2) + '</span></div>'
            : '') +
        '</div><div class="rftr">Thank you! &#127869; Grill N Shakes</div></div>';
}

function printZone(zoneId) {
    var html = $('#' + zoneId).html();
    var win = window.open('', '', 'width=420,height=680');
    win.document.write(
        '<html><head><title>Print</title><style>' +
        'body{font-family:"Courier New",monospace;margin:0;padding:10px}' +
        '.rh{text-align:center;border-bottom:2px dashed #ccc;padding-bottom:10px;margin-bottom:10px}' +
        '.rh h2{font-size:.9rem;font-weight:900;margin:0}.rh p{font-size:.65rem;color:#666;margin:2px 0 0}' +
        '.rbadge{display:inline-block;background:#e67e22;color:#fff;font-size:.62rem;font-weight:700;padding:2px 8px;border-radius:3px;margin-top:3px}' +
        '.rmeta{background:#f9f9f9;padding:6px;border-radius:4px;margin-bottom:8px;font-size:.68rem}' +
        '.rmr,.ri,.rtr{display:flex;justify-content:space-between;padding:2px 0}' +
        '.ri{font-size:.72rem;border-bottom:1px solid #f0f0f0;padding:3px 0}.ril{flex:1}' +
        '.riq{width:36px;font-weight:700;color:#555;font-size:.7rem}' +
        '.rtot{margin-top:8px;padding:8px;background:#f5f5f5;border-radius:4px;font-size:.72rem}' +
        '.rtr.grand{border-top:2px solid #333;padding-top:6px;margin-top:4px;font-weight:900;font-size:.84rem}' +
        '.rftr{text-align:center;border-top:2px dashed #ccc;padding-top:8px;margin-top:10px;font-size:.63rem;color:#888}' +
        '</style></head><body>' + html + '</body></html>'
    );
    win.document.close();
    win.focus();
    setTimeout(function () { win.print(); win.close(); }, 400);
}

function toast(msg, type) {
    var colors = { success: '#27ae60', error: '#c0392b', info: '#2980b9' };
    var $t = $('#noToast');
    $t.css('background', colors[type] || colors.success).html(msg).show();
    clearTimeout($t.data('timer'));
    $t.data('timer', setTimeout(function () { $t.hide(); }, 3000));
}