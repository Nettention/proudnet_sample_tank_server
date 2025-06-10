/**
 * WebGL Game Embed Manager
 * Handles loading Unity WebGL game directly with canvas
 */
class WebGLGameEmbed {
    constructor(containerId, baseUrl) {
        this.containerId = containerId;
        this.baseUrl = baseUrl;
        this.container = document.getElementById(containerId);
        this.canvas = null;
        this.unityInstance = null;
        this.isLoaded = false;
        this.loadAttempts = 0;
        this.maxLoadAttempts = 3;
        this.isKorean = document.documentElement.lang === 'ko' || document.title.includes('탱크 게임');
        
        this.init();
    }
    
    init() {
        if (!this.container) {
            console.error(`WebGL container with ID '${this.containerId}' not found`);
            return;
        }
        
        this.createGameInterface();
        this.setupEventListeners();
        // 자동으로 게임 로드 시작
        setTimeout(() => this.loadGame(), 500);
    }
    
    createGameInterface() {        
        const demoText = this.isKorean ? 
            '<strong>🎮 라이브 데모:</strong> ProudNet과 Unity로 구축된 실시간 멀티플레이어 탱크 게임입니다. WASD로 이동, 마우스로 조준, 클릭/스페이스로 발사하세요. 게임은 AWS에서 실행되는 라이브 서버에 연결됩니다.' :
            '<strong>🎮 Live Demo:</strong> This is a live multiplayer tank game built with ProudNet and Unity. Use WASD to move, mouse to aim, and click/space to fire. The game connects to a live server running on AWS.';
        
        this.container.innerHTML = `
            <div class="webgl-info">
                ${demoText}
            </div>
            
            <div id="webgl-canvas-area">
                <canvas id="unity-canvas" class="webgl-game-iframe" width="1280" height="720" tabindex="-1"></canvas>
                <div id="webgl-loading-overlay" class="webgl-loading-overlay">
                    <div class="webgl-loading-content">
                        <div class="webgl-loading-spinner"></div>
                        <p id="loading-text">${this.isKorean ? 'Unity WebGL 게임 로딩 중...' : 'Loading Unity WebGL game...'}</p>
                        <div id="loading-progress" class="webgl-progress-bar">
                            <div id="loading-progress-fill" class="webgl-progress-fill"></div>
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="webgl-game-controls">
                <button id="reload-game-btn" class="webgl-game-button" style="display: none;">${this.isKorean ? '🔄 새로고침' : '🔄 Reload'}</button>
                <button id="fullscreen-btn" class="webgl-game-button" style="display: none;">${this.isKorean ? '⛶ 전체화면' : '⛶ Fullscreen'}</button>
            </div>
            
            <div id="webgl-error-area"></div>
        `;
    }
    
    setupEventListeners() {
        const reloadBtn = document.getElementById('reload-game-btn');
        const fullscreenBtn = document.getElementById('fullscreen-btn');
        
        if (reloadBtn) {
            reloadBtn.addEventListener('click', () => this.reloadGame());
        }
        
        if (fullscreenBtn) {
            fullscreenBtn.addEventListener('click', () => this.toggleFullscreen());
        }
    }
    
    loadGame() {
        this.loadAttempts++;
        this.clearError();
        
        this.canvas = document.getElementById('unity-canvas');
        const loadingOverlay = document.getElementById('webgl-loading-overlay');
        const loadingText = document.getElementById('loading-text');
        const progressFill = document.getElementById('loading-progress-fill');
        
        if (!this.canvas) {
            this.onGameError('Canvas element not found');
            return;
        }
        
        // Show loading overlay
        if (loadingOverlay) {
            loadingOverlay.style.display = 'flex';
        }
        
        // Unity WebGL 설정
        const buildUrl = this.baseUrl + '/Build';
        const loaderUrl = buildUrl + '/Build_webgl.loader.js';
        
        const config = {
            dataUrl: buildUrl + '/Build_webgl.data',
            frameworkUrl: buildUrl + '/Build_webgl.framework.js',
            codeUrl: buildUrl + '/Build_webgl.wasm',
            streamingAssetsUrl: this.baseUrl + '/StreamingAssets',
            companyName: 'DefaultCompany',
            productName: 'unity_tank_client',
            productVersion: '1.0',
        };
        
        // Unity 로더 스크립트 동적 로드
        const script = document.createElement('script');
        script.src = loaderUrl;
        script.onload = () => {
            if (typeof createUnityInstance !== 'undefined') {
                // Unity 인스턴스 생성
                createUnityInstance(this.canvas, config, (progress) => {
                    // 로딩 진행률 업데이트
                    if (progressFill) {
                        progressFill.style.width = (100 * progress) + '%';
                    }
                    if (loadingText) {
                        loadingText.textContent = this.isKorean ? 
                            `로딩 중... ${Math.round(progress * 100)}%` : 
                            `Loading... ${Math.round(progress * 100)}%`;
                    }
                }).then((unityInstance) => {
                    this.unityInstance = unityInstance;
                    this.onGameLoaded();
                }).catch((message) => {
                    this.onGameError(this.isKorean ? 
                        'Unity 게임 로드 실패: ' + message : 
                        'Failed to load Unity game: ' + message);
                });
            } else {
                this.onGameError(this.isKorean ? 
                    'Unity 로더를 사용할 수 없습니다' : 
                    'Unity loader not available');
            }
        };
        
        script.onerror = () => {
            this.onGameError(this.isKorean ? 
                'Unity 로더 스크립트 로드에 실패했습니다. 인터넷 연결을 확인해주세요.' : 
                'Failed to load Unity loader script. Please check your internet connection.');
        };
        
        document.head.appendChild(script);
    }
    
    onGameLoaded() {
        this.isLoaded = true;
        
        const loadingOverlay = document.getElementById('webgl-loading-overlay');
        const reloadBtn = document.getElementById('reload-game-btn');
        const fullscreenBtn = document.getElementById('fullscreen-btn');
        
        // 로딩 오버레이 숨기기
        if (loadingOverlay) {
            loadingOverlay.style.display = 'none';
        }
        
        if (reloadBtn) {
            reloadBtn.style.display = 'inline-block';
        }
        
        if (fullscreenBtn) {
            fullscreenBtn.style.display = 'inline-block';
        }
        
        console.log('WebGL game loaded successfully');
    }
    
    onGameError(message) {
        const errorArea = document.getElementById('webgl-error-area');
        const loadingOverlay = document.getElementById('webgl-loading-overlay');
        
        // 로딩 오버레이 숨기기
        if (loadingOverlay) {
            loadingOverlay.style.display = 'none';
        }
        
        if (errorArea) {
            const errorLabel = this.isKorean ? '오류:' : 'Error:';
            const retryMessage = this.loadAttempts < this.maxLoadAttempts ? 
                (this.isKorean ? '<br><small>페이지를 새로고침하여 다시 시도할 수 있습니다.</small>' : '<br><small>You can try reloading the page to try again.</small>') : 
                (this.isKorean ? '<br><small>최대 로드 시도 횟수에 도달했습니다. 페이지를 새로고침해주세요.</small>' : '<br><small>Maximum load attempts reached. Please refresh the page.</small>');
            
            errorArea.innerHTML = `
                <div class="webgl-error">
                    <strong>${errorLabel}</strong> ${message}
                    ${retryMessage}
                </div>
            `;
        }
        
        console.error('WebGL game error:', message);
    }
    
    reloadGame() {
        const loadingOverlay = document.getElementById('webgl-loading-overlay');
        const loadingText = document.getElementById('loading-text');
        const progressFill = document.getElementById('loading-progress-fill');
        const reloadBtn = document.getElementById('reload-game-btn');
        const fullscreenBtn = document.getElementById('fullscreen-btn');
        
        if (reloadBtn) reloadBtn.style.display = 'none';
        if (fullscreenBtn) fullscreenBtn.style.display = 'none';
        
        this.clearError();
        this.isLoaded = false;
        
        // 로딩 오버레이 다시 표시
        if (loadingOverlay) {
            loadingOverlay.style.display = 'flex';
        }
        if (loadingText) {
            loadingText.textContent = 'Reloading game...';
        }
        if (progressFill) {
            progressFill.style.width = '0%';
        }
        
        // Unity 인스턴스 정리
        if (this.unityInstance) {
            try {
                this.unityInstance.Quit();
            } catch (e) {
                console.warn('Error quitting Unity instance:', e);
            }
            this.unityInstance = null;
        }
        
        // 게임 다시 로드
        setTimeout(() => this.loadGame(), 500);
    }
    
    toggleFullscreen() {
        if (!this.canvas) return;
        
        if (this.unityInstance) {
            this.unityInstance.SetFullscreen(1);
        } else if (this.canvas.requestFullscreen) {
            this.canvas.requestFullscreen();
        } else if (this.canvas.webkitRequestFullscreen) {
            this.canvas.webkitRequestFullscreen();
        } else if (this.canvas.msRequestFullscreen) {
            this.canvas.msRequestFullscreen();
        }
    }
    
    clearError() {
        const errorArea = document.getElementById('webgl-error-area');
        if (errorArea) {
            errorArea.innerHTML = '';
        }
    }
}

// Initialize WebGL game embed when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Check if we're on the correct page and container exists
    const gameContainer = document.getElementById('webgl-game-demo');
    if (gameContainer) {
        // Initialize the WebGL game embed
        const baseUrl = 'https://playdapp-members.s3.ap-northeast-2.amazonaws.com/game_build/proudnet-tank-game';
        window.webglGame = new WebGLGameEmbed('webgl-game-demo', baseUrl);
    }
}); 