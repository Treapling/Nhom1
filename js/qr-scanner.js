// === ĐÁNH THỨC KHO GIỌNG NÓI CỦA TRÌNH DUYỆT (SỬA LỖI GIỌNG TIẾNG ANH) ===
let voices = [];
function loadVoicesSafe() {
    voices = window.speechSynthesis.getVoices();
}
// Chạy kích hoạt lần đầu
loadVoicesSafe();
// Ép trình duyệt cập nhật lại danh sách ngay khi nạp xong ngầm
if (window.speechSynthesis.onvoiceschanged !== undefined) {
    window.speechSynthesis.onvoiceschanged = loadVoicesSafe;
}

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
                        readTextToSpeech(descText, currentLang);
                    }
                }
            })
            .catch(error => alert("Lỗi: " + error.message));
    }

    // === 3. ĐỌC VĂN BẢN (TTS) ĐA NGÔN NGỮ CHUẨN QUỐC TỊCH ===
    function readTextToSpeech(text, lang) {
        if ('speechSynthesis' in window) {
            window.speechSynthesis.cancel(); 
            const utterance = new SpeechSynthesisUtterance(text);
            
            // Định dạng tag chuẩn: en -> en-US, vi -> vi-VN
            const targetLang = (lang === 'en') ? 'en-US' : 'vi-VN';
            utterance.lang = targetLang;

            // Cập nhật lại kho giọng nói một lần nữa cho chắc chắn
            loadVoicesSafe();

            // Tìm kiếm thông minh: Chuyển hết về chữ thường và thay dấu gạch dưới thành gạch ngang để so khớp
            const matchedVoice = voices.find(v => {
                const voiceLang = v.lang.replace('_', '-').toLowerCase();
                return voiceLang.includes(targetLang.toLowerCase());
            });
            
            // Nếu tìm thấy giọng chuẩn (Microsoft Anoki hoặc Google Tiếng Việt) thì ép chạy giọng đó
            if (matchedVoice) {
                utterance.voice = matchedVoice;
            } else {
                // Phương án dự phòng: Tìm bất kỳ giọng nào có chứa chữ 'vi'
                const fallbackVoice = voices.find(v => v.lang.toLowerCase().includes('vi'));
                if (fallbackVoice) utterance.voice = fallbackVoice;
            }

            window.speechSynthesis.speak(utterance);
        }
    }

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