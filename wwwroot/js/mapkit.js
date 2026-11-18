/*
 * MapKit — tiny map adapter used across BloodConnect BD.
 * Uses Google Maps when an API key is configured (window.APP_MAPS_KEY),
 * otherwise falls back to Leaflet + OpenStreetMap so the app works on any PC.
 *
 *   MapKit.create('mapDiv', { center: [23.78, 90.40], zoom: 12 }).then(map => {
 *       const layer = map.layer();
 *       layer.add(map.marker(lat, lng, { color: '#dc3545', label: 'O+', html: '<b>Hi</b>' }));
 *       map.circle(lat, lng, 10, { color: '#dc3545' });
 *       map.onClick((lat, lng) => ...);
 *   });
 */
window.MapKit = (function () {
    'use strict';
    var key = window.APP_MAPS_KEY || '';
    var loading = null;
    var googleFailed = false;

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function loadScript(src) {
        return new Promise(function (resolve, reject) {
            var s = document.createElement('script');
            s.src = src; s.async = true; s.onload = resolve; s.onerror = reject;
            document.head.appendChild(s);
        });
    }

    function loadLeaflet() {
        if (window.L) return Promise.resolve('leaflet');
        var l = document.createElement('link');
        l.rel = 'stylesheet';
        l.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
        document.head.appendChild(l);
        return loadScript('https://unpkg.com/leaflet@1.9.4/dist/leaflet.js').then(function () { return 'leaflet'; });
    }

    // Google calls this if the key is invalid / billing is not enabled
    window.gm_authFailure = function () {
        googleFailed = true;
        console.warn('Google Maps authentication failed — check the API key restrictions and billing.');
        document.querySelectorAll('[data-mapkit]').forEach(function (el) {
            var note = document.createElement('div');
            note.className = 'alert alert-warning small m-2';
            note.style.position = 'absolute'; note.style.zIndex = 5; note.style.top = '0'; note.style.left = '0'; note.style.right = '0';
            note.textContent = 'Google Maps could not load (API key/billing). Reload the page to use OpenStreetMap instead.';
            el.appendChild(note);
        });
        try { sessionStorage.setItem('mapkit-google-failed', '1'); } catch (e) { }
    };

    function load() {
        if (loading) return loading;
        var failedBefore = false;
        try { failedBefore = sessionStorage.getItem('mapkit-google-failed') === '1'; } catch (e) { }
        if (key && !failedBefore) {
            loading = new Promise(function (resolve, reject) {
                window.__mapkitGoogleReady = function () { resolve('google'); };
                loadScript('https://maps.googleapis.com/maps/api/js?key=' + encodeURIComponent(key) +
                    '&callback=__mapkitGoogleReady&v=weekly&loading=async').catch(reject);
                setTimeout(function () { reject(new Error('timeout')); }, 8000);
            }).catch(function () { return loadLeaflet(); });
        } else {
            loading = loadLeaflet();
        }
        return loading;
    }

    // ------------------------------------------------------------------ Google implementation
    function googleMap(el, opts) {
        var g = google.maps;
        var map = new g.Map(el, {
            center: { lat: opts.center[0], lng: opts.center[1] },
            zoom: opts.zoom || 7,
            mapTypeControl: false, streetViewControl: false, fullscreenControl: true,
            clickableIcons: false
        });
        var info = new g.InfoWindow();

        function handle(obj) {
            return {
                remove: function () { obj.setMap(null); },
                show: function (v) { obj.setMap(v ? map : null); },
                open: function () { if (obj.__html) { info.setContent(obj.__html); info.open({ map: map, anchor: obj }); } },
                setPosition: function (lat, lng) { obj.setPosition ? obj.setPosition({ lat: lat, lng: lng }) : obj.setCenter({ lat: lat, lng: lng }); }
            };
        }

        return {
            engine: 'google',
            marker: function (lat, lng, o) {
                o = o || {};
                var m = new g.Marker({
                    position: { lat: lat, lng: lng }, map: map, title: o.title || '',
                    zIndex: o.zIndex || 1,
                    label: o.label ? { text: String(o.label), color: '#fff', fontSize: '10px', fontWeight: '700' } : null,
                    icon: {
                        path: g.SymbolPath.CIRCLE, scale: o.size || 14,
                        fillColor: o.color || '#dc3545', fillOpacity: 1, strokeColor: '#fff', strokeWeight: 2
                    }
                });
                if (o.html) {
                    m.__html = o.html;
                    m.addListener('click', function () { info.setContent(o.html); info.open({ map: map, anchor: m }); });
                }
                return handle(m);
            },
            circle: function (lat, lng, km, o) {
                o = o || {};
                var c = new g.Circle({
                    map: map, center: { lat: lat, lng: lng }, radius: km * 1000,
                    strokeColor: o.color || '#dc3545', strokeOpacity: .7, strokeWeight: 2,
                    fillColor: o.color || '#dc3545', fillOpacity: o.fillOpacity == null ? .06 : o.fillOpacity, clickable: false
                });
                var h = handle(c);
                h.setRadius = function (k) { c.setRadius(k * 1000); };
                return h;
            },
            onClick: function (fn) { map.addListener('click', function (e) { fn(e.latLng.lat(), e.latLng.lng()); }); },
            setView: function (lat, lng, zoom) { map.setCenter({ lat: lat, lng: lng }); if (zoom) map.setZoom(zoom); },
            fit: function (points) {
                if (!points.length) return;
                if (points.length === 1) { map.setCenter({ lat: points[0][0], lng: points[0][1] }); map.setZoom(13); return; }
                var b = new g.LatLngBounds();
                points.forEach(function (p) { b.extend({ lat: p[0], lng: p[1] }); });
                map.fitBounds(b, 40);
            },
            layer: layerFactory
        };
    }

    // ------------------------------------------------------------------ Leaflet implementation
    function leafletMap(el, opts) {
        var map = L.map(el).setView(opts.center, opts.zoom || 7);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors', maxZoom: 18
        }).addTo(map);

        function handle(obj) {
            return {
                remove: function () { map.removeLayer(obj); },
                show: function (v) { if (v) obj.addTo(map); else map.removeLayer(obj); },
                open: function () { if (obj.openPopup) obj.openPopup(); },
                setPosition: function (lat, lng) { obj.setLatLng([lat, lng]); }
            };
        }

        return {
            engine: 'leaflet',
            marker: function (lat, lng, o) {
                o = o || {};
                var size = (o.size || 14) * 2 + 4;
                var icon = L.divIcon({
                    className: '',
                    html: '<div class="' + (o.pulse ? 'pulse-marker' : '') + '" style="background:' + (o.color || '#dc3545') +
                        ';color:#fff;border-radius:50%;width:' + size + 'px;height:' + size + 'px;display:flex;align-items:center;' +
                        'justify-content:center;font-weight:700;font-size:10px;border:2px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.3)">' +
                        esc(o.label || '') + '</div>',
                    iconSize: [size, size], iconAnchor: [size / 2, size / 2]
                });
                var m = L.marker([lat, lng], { icon: icon, title: o.title || '', zIndexOffset: (o.zIndex || 0) * 100 }).addTo(map);
                if (o.html) m.bindPopup(o.html);
                return handle(m);
            },
            circle: function (lat, lng, km, o) {
                o = o || {};
                var c = L.circle([lat, lng], {
                    radius: km * 1000, color: o.color || '#dc3545', weight: 2,
                    fillOpacity: o.fillOpacity == null ? .06 : o.fillOpacity, interactive: false
                }).addTo(map);
                var h = handle(c);
                h.setRadius = function (k) { c.setRadius(k * 1000); };
                return h;
            },
            onClick: function (fn) { map.on('click', function (e) { fn(e.latlng.lat, e.latlng.lng); }); },
            setView: function (lat, lng, zoom) { map.setView([lat, lng], zoom || map.getZoom()); },
            fit: function (points) {
                if (!points.length) return;
                if (points.length === 1) { map.setView(points[0], 13); return; }
                map.fitBounds(points, { padding: [40, 40] });
            },
            layer: layerFactory
        };
    }

    function layerFactory() {
        var items = [], visible = true;
        return {
            add: function (h) { items.push(h); if (!visible) h.show(false); return h; },
            clear: function () { items.forEach(function (h) { h.remove(); }); items = []; },
            show: function (v) { visible = v; items.forEach(function (h) { h.show(v); }); },
            count: function () { return items.length; }
        };
    }

    return {
        esc: esc,
        create: function (elOrId, opts) {
            var el = typeof elOrId === 'string' ? document.getElementById(elOrId) : elOrId;
            el.setAttribute('data-mapkit', '1');
            el.style.position = el.style.position || 'relative';
            opts = opts || { center: [23.685, 90.3563], zoom: 7 };
            return load().then(function (engine) {
                return engine === 'google' && !googleFailed ? googleMap(el, opts) : leafletMap(el, opts);
            });
        }
    };
})();
