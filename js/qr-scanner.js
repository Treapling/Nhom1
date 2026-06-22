document.addEventListener("DOMContentLoaded", function() {
    const btnScanQR = document.getElementById('btn-scan-qr');
    const btnCloseQR = document.getElementById('btn-close-qr');
    const qrModal = document.getElementById('qr-modal');
    
    let html5QrCode = null;

    btnScanQR.addEventListener('click', () => {
        qrModal.classList.remove('hidden');
        
        Html5Qrcode.getCameras().then(devices => {
            if (devices && devices.length > 0) {
                const cameraId = devices[0].id; 
                html5QrCode = new Html5Qrcode("qr-reader");
                const config = { fps: 10, qrbox: { width: 250, height: 250 } };
                
                html5QrCode.start(cameraId, config, onScanSuccess)
                    .catch(err => alert("Lỗi phần cứng Camera: " + err));
            } else {
                alert("Không tìm thấy Camera!");
            }
        }).catch(err => alert("Trình duyệt chặn Camera: " + err));
    });

    function onScanSuccess(decodedText, decodedResult) {
        stopCamera().then(() => {
            qrModal.classList.add('hidden');
            
            const poiId = parseInt(decodedText);
            
            fetch(`http://localhost:5555/api/POI/${poiId}`)
                .then(response => {
                    if (!response.ok) throw new Error('Không tìm thấy địa điểm trong CSDL!');
                    return response.json(); 
                })
                .then(poiInfo => {
                    const poiCard = document.getElementById('poi-card');
                    const poiName = document.getElementById('poi-name');
                    const poiDesc = document.getElementById('poi-desc');
                    
                    poiCard.classList.remove('hidden');
                    poiName.innerText = poiInfo.name || poiInfo.Name;
                    poiDesc.innerText = poiInfo.description || poiInfo.Description;

                    readTextToSpeech(poiDesc.innerText);
                })
                .catch(error => alert("Lỗi: " + error.message));
        });
    }

    function readTextToSpeech(text) {
        const isAutoPlayEnabled = document.getElementById('toggle-tts').checked;
        
        if (!isAutoPlayEnabled) {
            return; 
        }

        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel(); 
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = 'vi-VN'; 
            window.speechSynthesis.speak(utterance);
        }
    }

    function stopCamera() {
        if (html5QrCode && html5QrCode.isScanning) {
            return html5QrCode.stop().then(() => {
                html5QrCode.clear(); 
            }).catch(err => console.error("Lỗi tắt camera:", err));
        }
        return Promise.resolve(); 
    }
});