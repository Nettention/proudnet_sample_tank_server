// 이미지 모달 기능
function openImageModal(imageSrc, imageAlt) {
    const modal = document.getElementById('imageModal');
    const modalImage = document.getElementById('modalImage');
    const modalCaption = document.getElementById('modalCaption');
    
    modalImage.src = imageSrc;
    modalImage.alt = imageAlt;
    modalCaption.textContent = imageAlt;
    
    modal.classList.add('active');
    document.body.style.overflow = 'hidden'; // 스크롤 방지
}

function closeImageModal() {
    const modal = document.getElementById('imageModal');
    modal.classList.remove('active');
    document.body.style.overflow = 'auto'; // 스크롤 복원
}

// 이미지 클릭 이벤트 등록
document.addEventListener('DOMContentLoaded', function() {
    const images = document.querySelectorAll('.image');
    
    images.forEach(function(image) {
        // 이미지 로드 완료 후 원본 크기 확인
        image.addEventListener('load', function() {
            // 이미지가 작은 경우 (400px 미만) 원본 크기 유지, 그 외는 적당한 크기로 제한
            if (this.naturalWidth < 400) {
                this.style.maxWidth = this.naturalWidth + 'px';
            } else if (this.naturalWidth < 600) {
                this.style.maxWidth = '500px';
            }
            // 600px 이상은 CSS의 max-width: 600px 적용
        });
        
        // 이미지가 이미 로드된 경우 (캐시된 이미지)
        if (image.complete && image.naturalHeight !== 0) {
            if (image.naturalWidth < 400) {
                image.style.maxWidth = image.naturalWidth + 'px';
            } else if (image.naturalWidth < 600) {
                image.style.maxWidth = '500px';
            }
        }
        
        image.addEventListener('click', function() {
            openImageModal(this.src, this.alt);
        });
    });

    // 모달 배경 클릭 시 닫기
    const modal = document.getElementById('imageModal');
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                closeImageModal();
            }
        });
    }

    // ESC 키로 모달 닫기
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            closeImageModal();
        }
    });
}); 