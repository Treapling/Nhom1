document.addEventListener("DOMContentLoaded", function() {
    // === 1. LOGIC BẢN ĐỒ VÀ ĐỊNH VỊ ===
    const map = L.map('map').setView([10.7570, 106.6990], 17);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    setTimeout(() => { map.invalidateSize(); }, 500);

    let userMarker = L.marker([10.7570, 106.6990]).addTo(map);
    userMarker.bindPopup("Vị trí của bạn").openPopup();

    // Lấy thông tin sạp hàng ID = 6 và hiển thị lên bản đồ kèm mã QR
    fetch('/api/POI/6')
        .then(res => {
            if(res.ok) return res.json();
            throw new Error("Không tìm thấy POI 6");
        })
        .then(poi => {
            const poiId = poi.id || poi.Id;
            const poiName = poi.name || poi.Name;
            const lat = poi.lat || poi.Lat;
            const lng = poi.lng || poi.Lng;
            
            if(lat && lng) {
                const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${poiId}`;
                const popupContent = `
                    <div style="text-align: center; min-width: 160px;">
                        <h4 style="margin: 0 0 5px 0; font-weight: bold; color: #1e3a8a;">${poiName}</h4>
                        <p style="margin: 0 0 10px 0; font-size: 12px; color: #6b7280;">Quét mã QR để nghe thông tin</p>
                        <img src="${qrUrl}" alt="QR Code" style="width: 150px; height: 150px; margin: 0 auto; border-radius: 8px;" />
                    </div>
                `;
                L.marker([lat, lng]).addTo(map).bindPopup(popupContent);
            }
        })
        .catch(err => console.error("Lỗi khi tải POI 6:", err));

    const poiCard = document.getElementById('poi-card');
    const poiName = document.getElementById('poi-name'); 
    const poiDesc = document.getElementById('poi-desc');
    const audioPlayer = document.getElementById('audio-player');

    // Biến toàn cục để qr-scanner có thể ghi đè
    window.lastPlayedPoiId = null;

    if ("geolocation" in navigator) {
        let lastCheckTime = 0;

        navigator.geolocation.watchPosition(
            function(position) {
                // Giả lập cố định vị trí ở Phố ẩm thực Vĩnh Khánh
                const lat = 10.7570;
                const lng = 106.6990;
                const newLatLng = new L.LatLng(lat, lng);

                if (userMarker) {
                    userMarker.setLatLng(newLatLng);
                } else {
                    userMarker = L.marker(newLatLng).addTo(map).bindPopup("Vị trí của bạn").openPopup();
                }
                map.panTo(newLatLng);

                const currentTime = Date.now();
                if (currentTime - lastCheckTime > 5000) {
                    lastCheckTime = currentTime; 
                    fetch(`/api/location/check?lat=${lat}&lng=${lng}`)
                        .then(response => response.json())
                        .then(data => {
                            if (data.triggered === true) {
                                poiCard.classList.remove('hidden');
                                if (poiName) poiName.innerText = data.poi.name;
                                if (poiDesc) poiDesc.innerText = data.poi.description;

                                if (data.poi.id !== window.lastPlayedPoiId) {
                                    if (audioPlayer) {
                                        audioPlayer.src = data.audioUrl;
                                        audioPlayer.onerror = () => {
                                            const utterance = new SpeechSynthesisUtterance(data.poi.description);
                                            utterance.lang = 'vi-VN'; 
                                            window.speechSynthesis.speak(utterance);
                                        };
                                        audioPlayer.play().catch(err => console.warn("Chặn âm thanh:", err));
                                    }
                                    window.lastPlayedPoiId = data.poi.id;

                                    // Reset cooldown định vị sau 5 phút (300000 ms)
                                    setTimeout(() => {
                                        if (window.lastPlayedPoiId === data.poi.id) {
                                            window.lastPlayedPoiId = null;
                                        }
                                    }, 300000);
                                }
                            } else {
                                poiCard.classList.add('hidden');
                            }
                        })
                        .catch(error => console.error("Lỗi API Định vị:", error));
                }
            },
            function(error) { console.warn(`Lỗi định vị: ${error.message}`); },
            { enableHighAccuracy: true, maximumAge: 0, timeout: 5000 }
        );
    } else {
        console.error("Trình duyệt không hỗ trợ Geolocation.");
    }

    // === 2. LOGIC ĐÓNG THẺ POI CỦA BẠN ===
    const btnClosePoi = document.getElementById('close-poi-btn');
    btnClosePoi.addEventListener('click', () => {
        poiCard.classList.add('hidden'); 
        window.speechSynthesis.cancel(); 
    });

    // === 3. LOGIC CHUYỂN TAB FOOTER ===
    const viewMap = document.getElementById('view-map');
    const viewHistory = document.getElementById('view-history');
    const viewSettings = document.getElementById('view-settings');

    const tabMap = document.getElementById('tab-map');
    const tabHistory = document.getElementById('tab-history');
    const tabSettings = document.getElementById('tab-settings');

    function resetTabs() {
        viewMap.style.opacity = '0'; viewMap.style.pointerEvents = 'none';
        viewHistory.classList.add('hidden');
        viewSettings.classList.add('hidden');
        
        tabMap.classList.replace('text-blue-600', 'text-gray-400');
        tabHistory.classList.replace('text-blue-600', 'text-gray-400');
        tabSettings.classList.replace('text-blue-600', 'text-gray-400');
    }

    tabMap.addEventListener('click', () => {
        resetTabs();
        viewMap.style.opacity = '1'; viewMap.style.pointerEvents = 'auto';
        tabMap.classList.replace('text-gray-400', 'text-blue-600');
    });

    tabHistory.addEventListener('click', () => {
        resetTabs();
        viewHistory.classList.remove('hidden');
        tabHistory.classList.replace('text-gray-400', 'text-blue-600');
    });

    tabSettings.addEventListener('click', () => {
        resetTabs();
        viewSettings.classList.remove('hidden');
        tabSettings.classList.replace('text-gray-400', 'text-blue-600');
    });
});