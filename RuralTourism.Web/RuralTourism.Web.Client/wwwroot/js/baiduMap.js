window.initBaiduMap = (elementId, lat, lng, title) => {
    if (typeof BMapGL === 'undefined') {
        console.error("Baidu Map API not loaded.");
        return;
    }
    const map = new BMapGL.Map(elementId);
    const point = new BMapGL.Point(lng, lat);
    map.centerAndZoom(point, 15);
    map.enableScrollWheelZoom(true);

    const marker = new BMapGL.Marker(point);
    map.addOverlay(marker);

    if (title) {
        const titleLabel = new BMapGL.Label(title, { offset: new BMapGL.Size(20, -10) });
        titleLabel.setStyle({
            color: "#333",
            border: "1px solid #ccc",
            padding: "2px 5px",
            borderRadius: "3px",
            backgroundColor: "#fff"
        });
        marker.setLabel(titleLabel);
    }
}

window.planBaiduRoute = (mapElementId, panelElementId, pointsStr) => {
    if (typeof BMapGL === 'undefined') {
        console.error("Baidu Map API not loaded.");
        return;
    }
    const map = new BMapGL.Map(mapElementId);
    map.enableScrollWheelZoom(true);
    
    const points = JSON.parse(pointsStr);
    if (!points || points.length < 2) return;
    
    const startPoint = new BMapGL.Point(points[0].lng, points[0].lat);
    const endPoint = new BMapGL.Point(points[points.length - 1].lng, points[points.length - 1].lat);
    map.centerAndZoom(startPoint, 13);
    
    const waypoints = points.slice(1, points.length - 1).map(p => new BMapGL.Point(p.lng, p.lat));
    
    const driving = new BMapGL.DrivingRoute(map, {
        renderOptions: {
            map: map,
            panel: panelElementId,
            autoViewport: true
        }
    });
    
    driving.search(startPoint, endPoint, { waypoints: waypoints });
}

window.getBaiduLocation = (address, city) => {
    return new Promise((resolve, reject) => {
        if (typeof BMapGL === 'undefined') {
            resolve(null);
            return;
        }
        const myGeo = new BMapGL.Geocoder();
        myGeo.getPoint(address, function(point){
            if (point) {
                resolve({ lng: point.lng, lat: point.lat });
            } else {
                resolve(null); // return null if not found
            }
        }, city || "镇江市");
    });
}

window.initBaiduCityMap = (elementId, cityName) => {
    if (typeof BMapGL === 'undefined') {
        console.error("Baidu Map API not loaded.");
        return;
    }
    const map = new BMapGL.Map(elementId);
    map.centerAndZoom(cityName || "镇江", 12);
    map.enableScrollWheelZoom(true);
}

window.planBaiduRouteAdvanced = (mapElementId, panelElementId, pointsStr, mode) => {
    if (typeof BMapGL === 'undefined') {
        console.error("Baidu Map API not loaded.");
        return;
    }
    const map = new BMapGL.Map(mapElementId);
    map.enableScrollWheelZoom(true);

    const panel = document.getElementById(panelElementId);
    if (panel) panel.innerHTML = '';

    const points = JSON.parse(pointsStr || '[]');
    if (!points || points.length < 2) {
        map.centerAndZoom("镇江", 12);
        return;
    }

    const bPoints = points.map(p => new BMapGL.Point(p.lng, p.lat));
    map.centerAndZoom(bPoints[0], 13);

    const routeMode = (mode || 'driving').toLowerCase();
    if (routeMode === 'transit') {
        for (let i = 0; i < bPoints.length - 1; i++) {
            const transit = new BMapGL.TransitRoute(map, {
                renderOptions: {
                    map: map,
                    autoViewport: i === 0
                }
            });
            transit.search(bPoints[i], bPoints[i + 1]);
        }
        return;
    }

    const startPoint = bPoints[0];
    const endPoint = bPoints[bPoints.length - 1];
    const waypoints = bPoints.slice(1, bPoints.length - 1);
    const driving = new BMapGL.DrivingRoute(map, {
        renderOptions: {
            map: map,
            panel: panelElementId,
            autoViewport: true
        }
    });
    driving.search(startPoint, endPoint, { waypoints: waypoints });
}
