// TÍNH NĂNG TỰ ĐỘNG PHÁT ÂM THANH THEO NGÔN NGỮ (CHỈ DÙNG FILE AUDIO)

document.addEventListener("DOMContentLoaded", function() {
    const btnScanQR = document.getElementById('btn-scan-qr');
    const btnCloseQR = document.getElementById('btn-close-qr');
    const qrModal = document.getElementById('qr-modal');
    
    let html5QrCode = null;

    // === 1. MỞ CAMERA ===
    btnScanQR.addEventListener('click', () => {
        qrModal.classList.remove('hidden');
        
        html5QrCode = new Html5Qrcode("qr-reader");
        const config = { fps: 10, qrbox: { width: 250, height: 250 } };
        
        // Ưu tiên mở camera sau (môi trường ngoài)
        html5QrCode.start({ facingMode: "environment" }, config, onScanSuccess)
            .catch(err => {
                // Nếu điện thoại không hỗ trợ hoặc đang dùng Laptop, dùng camera đầu tiên tìm được
                Html5Qrcode.getCameras().then(devices => {
                    if (devices && devices.length > 0) {
                        html5QrCode.start(devices[0].id, config, onScanSuccess)
                            .catch(e => alert("Lỗi phần cứng Camera: " + e));
                    } else {
                        alert("Không tìm thấy Camera!");
                    }
                }).catch(e => alert("Trình duyệt chặn Camera: " + e));
            });
    });

    // === 2. XỬ LÝ KHI QUÉT THÀNH CÔNG ===
    function onScanSuccess(decodedText, decodedResult) {
        qrModal.classList.add('hidden');
        stopCameraSafe();
        
        const poiId = parseInt(decodedText);
        
        fetch(`/api/POI/${poiId}`)
            .then(response => {
                if (!response.ok) throw new Error('Không tìm thấy địa điểm trong CSDL!');
                return response.json(); 
            })
            .then(poiInfo => {
                const poiCard = document.getElementById('poi-card');
                const poiName = document.getElementById('poi-name');
                const poiDesc = document.getElementById('poi-desc');
                const audioPlayer = document.getElementById('audio-player');
                
                poiCard.classList.remove('hidden');
                poiName.innerText = poiInfo.name || poiInfo.Name;
                const descText = poiInfo.description || poiInfo.Description;
                poiDesc.innerText = descText;

                // Cập nhật biến toàn cục để hệ thống định vị GPS không đọc lại ngay lập tức
                window.lastPlayedPoiId = poiInfo.id || poiInfo.Id;

                // KIỂM TRA LOGIC ÂM THANH ĐA NGÔN NGỮ
                const isAutoPlayEnabled = document.getElementById('toggle-tts').checked;
                const currentLang = document.getElementById('lang-selector').value; 

                if (isAutoPlayEnabled) {
                    let matchedAudioUrl = null;
                    if (poiInfo.audios && poiInfo.audios.length > 0) {
                        const matchedFile = poiInfo.audios.find(a => a.language === currentLang);
                        if (matchedFile) matchedAudioUrl = matchedFile.filePath || matchedFile.FilePath;
                    }

                    if (matchedAudioUrl) {
                        audioPlayer.src = matchedAudioUrl;
                        audioPlayer.play().catch(err => console.warn("Lỗi phát audio:", err));
                    } else {
                        console.warn("Không tìm thấy file audio cho ngôn ngữ: " + currentLang);
                    }
                }
            })
            .catch(error => alert("Lỗi: " + error.message));
    }

    // (Đã loại bỏ hàm readTextToSpeech và tính năng đọc TTS)


    // === 4. ĐÓNG CAMERA KHI BẤM NÚT ĐÓNG ===
    btnCloseQR.addEventListener('click', () => {
        qrModal.classList.add('hidden');
        stopCameraSafe();
    });

    // === 5. HÀM HỖ TRỢ TẮT CAMERA AN TOÀN ===
    function stopCameraSafe() {
        if (html5QrCode) {
            try {
                html5QrCode.stop().then(() => {
                    html5QrCode.clear(); 
                }).catch(err => {
                    // Kệ nó
                });
            } catch (error) {
                // Kệ nó
            }
        }
    }
});