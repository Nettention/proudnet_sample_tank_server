/**
 * Language Selector for Documentation
 * Handles language switching functionality
 */
class LanguageSelector {
    constructor() {
        this.currentLang = this.detectCurrentLanguage();
        this.languages = {
            'en': {
                name: 'English',
                flag: '🇺🇸',
                file: this.getCurrentDocumentType() + '.en.html'
            },
            'ko': {
                name: '한국어',
                flag: '🇰🇷', 
                file: this.getCurrentDocumentType() + '.kr.html'
            }
            // 추후 다른 언어 추가 가능:
            // 'ja': {
            //     name: '日本語',
            //     flag: '🇯🇵',
            //     file: '02.tank_client.ja.html'
            // },
            // 'zh': {
            //     name: '中文',
            //     flag: '🇨🇳',
            //     file: '02.tank_client.zh.html'
            // }
        };
        
        this.init();
    }
    
    getCurrentDocumentType() {
        const path = window.location.pathname;
        const filename = path.split('/').pop();
        
        if (filename.includes('01.server')) {
            return '01.server';
        } else if (filename.includes('02.tank_client')) {
            return '02.tank_client';
        } else if (filename.includes('03.unreal_shooter_sample')) {
            return '03.unreal_shooter_sample';
        }
        
        // 기본값
        return '02.tank_client';
    }
    
    detectCurrentLanguage() {
        const path = window.location.pathname;
        const filename = path.split('/').pop();
        
        if (filename.includes('.kr.') || filename.includes('_kr.')) {
            return 'ko';
        }
        // 기본값은 영어
        return 'en';
    }
    
    init() {
        this.createLanguageSelector();
        this.setupEventListeners();
    }
    
    createLanguageSelector() {
        const header = document.querySelector('.md-header');
        if (!header) return;
        
        const currentLangData = this.languages[this.currentLang];
        
        const selectorHTML = `
            <div class="language-selector">
                <div class="language-dropdown" id="languageDropdown">
                    <div class="language-toggle" id="languageToggle">
                        <span>
                            <span class="language-flag">${currentLangData.flag}</span>
                            ${currentLangData.name}
                        </span>
                        <span class="dropdown-arrow">▼</span>
                    </div>
                    <div class="language-dropdown-content">
                        ${this.generateLanguageOptions()}
                    </div>
                </div>
            </div>
        `;
        
        header.insertAdjacentHTML('beforeend', selectorHTML);
    }
    
    generateLanguageOptions() {
        return Object.entries(this.languages).map(([code, data]) => {
            const isCurrent = code === this.currentLang;
            return `
                <a href="${data.file}" class="language-option ${isCurrent ? 'current' : ''}" data-lang="${code}">
                    <span class="language-flag">${data.flag}</span>
                    ${data.name}
                </a>
            `;
        }).join('');
    }
    
    setupEventListeners() {
        const toggle = document.getElementById('languageToggle');
        const dropdown = document.getElementById('languageDropdown');
        
        if (toggle && dropdown) {
            toggle.addEventListener('click', (e) => {
                e.stopPropagation();
                dropdown.classList.toggle('active');
            });
            
            // 외부 클릭시 드롭다운 닫기
            document.addEventListener('click', () => {
                dropdown.classList.remove('active');
            });
            
            // 드롭다운 내부 클릭시 이벤트 전파 방지
            dropdown.addEventListener('click', (e) => {
                e.stopPropagation();
            });
        }
        
        // 언어 옵션 클릭 이벤트
        const languageOptions = document.querySelectorAll('.language-option');
        languageOptions.forEach(option => {
            option.addEventListener('click', (e) => {
                const targetLang = e.currentTarget.getAttribute('data-lang');
                if (targetLang !== this.currentLang) {
                    this.switchLanguage(targetLang);
                }
            });
        });
    }
    
    switchLanguage(targetLang) {
        if (this.languages[targetLang]) {
            // 현재 페이지 URL에서 새 언어 파일로 이동
            const newUrl = this.getNewUrl(targetLang);
            window.location.href = newUrl;
        }
    }
    
    getNewUrl(targetLang) {
        const currentUrl = window.location.href;
        const baseUrl = currentUrl.substring(0, currentUrl.lastIndexOf('/') + 1);
        return baseUrl + this.languages[targetLang].file;
    }
}

// DOM 로드 완료 후 언어 선택기 초기화
document.addEventListener('DOMContentLoaded', () => {
    new LanguageSelector();
}); 