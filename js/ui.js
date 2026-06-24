document.addEventListener("DOMContentLoaded", function() {
    // === 1. LOGIC BẢN ĐỒ VÀ ĐỊNH VỊ ===
    const map = L.map('map').setView([10.7570, 106.6990], 17);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    setTimeout(() => { map.invalidateSize(); }, 500);

    let userMarker = L.marker([10.7570, 106.6990]).addTo(map);
    userMarker.bindPopup("Vị trí của bạn").openPopup();

    const poiCard = document.getElementById('poi-card');
    const poiName = document.getElementById('poi-name'); 
    const poiDesc = document.getElementById('poi-desc');
    const audioPlayer = document.getElementById('audio-player');

    if ("geolocation" in navigator) {
        let lastCheckTime = 0;
        let lastPlayedPoiId = null;

        navigator.geolocation.watchPosition(
            function(position) {
                const lat = position.coords.latitude;
                const lng = position.coords.longitude;
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

                                if (data.poi.id !== lastPlayedPoiId) {
                                    if (audioPlayer) {
                                        audioPlayer.src = data.audioUrl;
                                        audioPlayer.onerror = () => {
                                            const utterance = new SpeechSynthesisUtterance(data.poi.description);
                                            utterance.lang = 'vi-VN'; 
                                            window.speechSynthesis.speak(utterance);
                                        };
                                        audioPlayer.play().catch(err => console.warn("Chặn âm thanh:", err));
                                    }
                                    lastPlayedPoiId = data.poi.id;
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