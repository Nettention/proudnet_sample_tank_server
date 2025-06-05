#include <iostream>
#include <map>
#include <vector>
#include <string>
#include <ctime>
#include <thread>
#include <mutex>
#include <chrono>
#include <algorithm>
#include <random>
#include <cmath>
#include <sstream>

// Windows 헤더 포함
#ifdef _WIN32
#include <windows.h>
#endif

// ProudNet headers
#include "ProudNetServer.h"

// include 디렉토리에 있는 파일들을 include합니다
#include "../include/Tank_common.cpp"
#include "../include/Tank_proxy.cpp"
#include "../include/Tank_stub.cpp"

// Common 디렉토리의 Vars.h를 include합니다
#include "../../Common/Vars.h"

using namespace std;
using namespace Proud;

// 디버그 출력 함수
inline void DebugLog(const std::string& message) {
    std::cout << message << std::endl;
}

// RmiContext 생성 함수
inline ::Proud::RmiContext CreateServerRmiContext() {
    ::Proud::RmiContext rmiCtx;
    rmiCtx.m_sentFrom = ::Proud::HostID_Server;
    return rmiCtx;
}


// TankInfo - 탱크 정보를 저장하는 구조체
struct TankInfo {
    int clientId;
    float posX;
    float posY;
    float direction;
    int tankType;
    float currentHealth;
    float maxHealth;
    bool isDestroyed;

    TankInfo(int _clientId = 0, float _posX = 0, float _posY = 0, float _direction = 0, 
            int _tankType = -1, float _maxHealth = 100.0f)
        : clientId(_clientId), posX(_posX), posY(_posY), direction(_direction),
          tankType(_tankType), maxHealth(_maxHealth), currentHealth(_maxHealth), isDestroyed(false) {
    }
};

// TankServer 클래스 - 탱크 게임 서버
class TankServer : public Tank::Stub {
private:
    // RMI 프록시 인스턴스
    Tank::Proxy tankProxy;
    
    // P2P 그룹 ID
    ::Proud::HostID gameP2PGroupID;
    
    // 연결된 탱크들의 정보
    std::map<::Proud::HostID, TankInfo> tanks;
    
    // 네트워크 서버 인스턴스
    std::shared_ptr<::Proud::CNetServer> server;
    
    // 뮤텍스 - 스레드 안전성 보장
    std::mutex mutex;
    
    // 초기화 함수
    void Initialize();
    
    // 클라이언트 접속 처리
    void OnClientJoin(::Proud::CNetClientInfo* clientInfo);
    
    // 클라이언트 접속 종료 처리
    void OnClientLeave(::Proud::CNetClientInfo* clientInfo, ::Proud::ErrorInfo* errorInfo, const ::Proud::ByteArray& comment);
    
    // P2P 그룹 업데이트
    void UpdateP2PGroup();
    
    // 커맨드 처리 루프
    void ProcessCommands();
    
    // 연결된 클라이언트 정보 출력
    void PrintConnectedClients();
    
    // 탱크 체력 정보 출력
    void ShowTankHealth(const string& input);
    
    // 탱크에 데미지 적용
    void ApplyDamageToTank(const string& input);
    
    // 탱크 치유
    void HealTank(const string& input);
    
    // 탱크 리스폰
    void RespawnTank(const string& input);
    
    // 클라이언트 ID로 HostID 찾기
    ::Proud::HostID FindHostIDById(int clientId);

public:
    TankServer();
    ~TankServer();
    
    // 서버 시작
    void Start();
    
    // RMI 스텁 메서드 정의 (Tank::Stub에서 상속)
#ifdef _WIN32
    // Windows에서는 클래스 이름 필요
    DEFRMI_Tank_SendMove(TankServer);
    DEFRMI_Tank_SendFire(TankServer);
    DEFRMI_Tank_SendTankType(TankServer);
    DEFRMI_Tank_SendTankHealthUpdated(TankServer);
    DEFRMI_Tank_SendTankDestroyed(TankServer);
    DEFRMI_Tank_SendTankSpawned(TankServer);
    DEFRMI_Tank_P2PMessage(TankServer);
#else
    // Linux에서는 매크로를 사용하지 않고 직접 선언
    bool SendMove(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const float& posX, const float& posY, const float& direction);
    bool SendFire(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const int& shooterId, const float& direction, const float& launchForce, const float& fireX, const float& fireY, const float& fireZ);
    bool SendTankType(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const int& tankType);
    bool SendTankHealthUpdated(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const float& currentHealth, const float& maxHealth);
    bool SendTankDestroyed(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const int& destroyedById);
    bool SendTankSpawned(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const float& posX, const float& posY, const float& direction, const int& tankType, const float& initialHealth);
    bool P2PMessage(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const ::Proud::String& message);
#endif
};

// 생성자
TankServer::TankServer() : gameP2PGroupID(::Proud::HostID_None) {
    // 서버 객체 생성 - shared_ptr로 래핑
    server = std::shared_ptr<::Proud::CNetServer>(::Proud::CNetServer::Create());
}

// 소멸자
TankServer::~TankServer() {
    if (server) {
        server->Stop();
    }
}

// 초기화 함수
void TankServer::Initialize() {
    // 스텁과 프록시를 서버에 연결
    server->AttachStub(this);
    server->AttachProxy(&tankProxy);

    // 클라이언트 접속 이벤트 핸들러
    server->OnClientJoin = [this](CNetClientInfo* clientInfo) {
        OnClientJoin(clientInfo);
    };

    // 클라이언트 접속 종료 이벤트 핸들러
    server->OnClientLeave = [this](CNetClientInfo* clientInfo, ErrorInfo* errorInfo, const ByteArray& comment) {
        OnClientLeave(clientInfo, errorInfo, comment);
    };
}

// 클라이언트 접속 처리
void TankServer::OnClientJoin(::Proud::CNetClientInfo* clientInfo) {
    std::lock_guard<std::mutex> lock(mutex);
    
    HostID hostId = clientInfo->m_HostID;
    
    // 랜덤 위치 생성 (예: 0~100 범위)
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_real_distribution<float> dis(0.0f, 100.0f);
    float posX = dis(gen);
    float posY = dis(gen);
    
    // 초기 탱크 타입은 -1로 설정 (선택 안함)
    int defaultTankType = -1;
    
    // 초기 체력 설정
    float defaultMaxHealth = 100.0f;
    float defaultHealth = defaultMaxHealth;
    
    // 탱크 정보 저장
    TankInfo newTank((int)hostId, posX, posY, 0, defaultTankType, defaultMaxHealth);
    tanks[hostId] = newTank;
    
    DebugLog("Client connected: Host ID = " + std::to_string(static_cast<int>(hostId)));
    DebugLog("New tank created for client " + std::to_string(static_cast<int>(hostId)) + " with tank type " + std::to_string(defaultTankType) 
         + " and health " + std::to_string(defaultHealth) + "/" + std::to_string(defaultMaxHealth));
    
    // 새 클라이언트에게 기존 탱크 정보 전송
    for (const auto& tank : tanks) {
        if (tank.first != hostId) { // 본인 제외
            // 기존 플레이어 정보 전송
            ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
            
            tankProxy.OnPlayerJoined(hostId, rmiCtx, (int)tank.first, 
                                    tank.second.posX, tank.second.posY, tank.second.tankType);
            
            // 기존 플레이어 체력 정보 전송
            tankProxy.OnTankHealthUpdated(hostId, rmiCtx, (int)tank.first, 
                                        tank.second.currentHealth, tank.second.maxHealth);
            
            // 기존 플레이어가 파괴 상태라면 파괴 정보도 전송
            if (tank.second.isDestroyed) {
                tankProxy.OnTankDestroyed(hostId, rmiCtx, (int)tank.first, 0); // 파괴자 ID 정보가 없으므로 0(환경)으로 설정
            }
            
            DebugLog("Sending existing player info to new client: ID=" + std::to_string(static_cast<int>(tank.first)) 
                 + ", Type=" + std::to_string(tank.second.tankType) 
                 + ", Health=" + std::to_string(tank.second.currentHealth) + "/" + std::to_string(tank.second.maxHealth));
        }
    }
    
    // 모든 클라이언트에게 새 플레이어 참가 알림
    for (const auto& existingClient : tanks) {
        // 새로 참가한 클라이언트 자신에게는 보내지 않음
        if (existingClient.first != hostId) {
            // 새 플레이어 정보 전송
            ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
            
            tankProxy.OnPlayerJoined(existingClient.first, rmiCtx, (int)hostId, 
                                    posX, posY, defaultTankType);
            
            // 새 플레이어 체력 정보 전송
            tankProxy.OnTankHealthUpdated(existingClient.first, rmiCtx, (int)hostId, 
                                        defaultHealth, defaultMaxHealth);
            
            // DebugLog("Notifying existing client " + std::to_string(static_cast<int>(existingClient.first)) + " about new player: ID=" + std::to_string(static_cast<int>(hostId)) 
                //  + ", Type=" + std::to_string(defaultTankType) + ", Health=" + std::to_string(defaultHealth) + "/" + std::to_string(defaultMaxHealth));
        }
    }
    
    // P2P 그룹 업데이트
    UpdateP2PGroup();
}

// 클라이언트 접속 종료 처리
void TankServer::OnClientLeave(::Proud::CNetClientInfo* clientInfo, ::Proud::ErrorInfo* errorInfo, const ::Proud::ByteArray& comment) {
    std::lock_guard<std::mutex> lock(mutex);
    
    HostID hostId = clientInfo->m_HostID;
    
    std::string errorMessage = "Unknown error";
    if (errorInfo != nullptr) {
        errorMessage = std::string(errorInfo->ToString());
    }
    
    DebugLog("Client " + std::to_string(static_cast<int>(hostId)) + " disconnected: " + errorMessage);
    
    // 탱크 정보 제거
    if (tanks.find(hostId) != tanks.end()) {
        tanks.erase(hostId);
    }
    
    // 모든 클라이언트에게 플레이어 퇴장 알림
    for (const auto& remainingClient : tanks) {
        ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
        
        tankProxy.OnPlayerLeft(remainingClient.first, rmiCtx, (int)hostId);
    }
    
    // P2P 그룹 업데이트
    UpdateP2PGroup();
}

// P2P 그룹 업데이트
void TankServer::UpdateP2PGroup() {
    // 기존 그룹 제거
    if (gameP2PGroupID != ::Proud::HostID_None) {
        server->DestroyP2PGroup(gameP2PGroupID);
        gameP2PGroupID = ::Proud::HostID_None;
    }
    
    // 새 그룹 생성 (2명 이상일 때)
    if (tanks.size() >= 2) {
        vector<::Proud::HostID> clients;
        for (const auto& tank : tanks) {
            clients.push_back(tank.first);
        }
        
        // Sample 코드 참조 - ByteArray 없이 호출
        gameP2PGroupID = server->CreateP2PGroup(&clients[0], clients.size());
        DebugLog("P2P group created with " + std::to_string(tanks.size()) + " members, Group ID: " + std::to_string(static_cast<int>(gameP2PGroupID)));
        
        // 모든 클라이언트에게 P2P 그룹 ID 알림
        for (const auto& clientPair : tanks) {
            ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
            
            // P2PMessage에 그룹 ID 정보 전송
            ::Proud::String groupInfoMsg;
            groupInfoMsg.Format(_PNT("P2P_GROUP_INFO:%d"), static_cast<int>(gameP2PGroupID));
            tankProxy.P2PMessage(clientPair.first, rmiCtx, groupInfoMsg);
            
            // DebugLog("Sent P2P group info to client " + std::to_string(static_cast<int>(clientPair.first)) + ": " + std::string(groupInfoMsg));
        }
    } else {
        DebugLog("Not enough clients to create P2P group (need at least 2)");
        gameP2PGroupID = ::Proud::HostID_None;
    }
}

// 위치 이동 요청 처리
#ifdef _WIN32
DEFRMI_Tank_SendMove(TankServer)
#else
bool TankServer::SendMove(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const float& posX, const float& posY, const float& direction)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("SendMove from client " + std::to_string(static_cast<int>(remote)) + ": pos=(" + std::to_string(posX) + "," + std::to_string(posY) 
         + "), direction=" + std::to_string(direction));
    
    // 탱크 정보 업데이트
    if (tanks.find(remote) != tanks.end()) {
        tanks[remote].posX = posX;
        tanks[remote].posY = posY;
        tanks[remote].direction = direction;
        
        // 모든 클라이언트에게 업데이트된 위치 전송
        for (const auto& clientPair : tanks) {
            // 움직인 클라이언트 자신에게는 보내지 않음
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                
                tankProxy.OnTankPositionUpdated(clientPair.first, rmiCtx, 
                                                (int)remote, posX, posY, direction);
            }
        }
    }
    
    return true;
}

// 발사 요청 처리
#ifdef _WIN32
DEFRMI_Tank_SendFire(TankServer)
#else
bool TankServer::SendFire(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const int& shooterId, const float& direction, const float& launchForce, const float& fireX, const float& fireY, const float& fireZ)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("========== SendFire Received ==========");
    DebugLog("From client " + std::to_string(static_cast<int>(remote)) + ": shooterId=" + std::to_string(shooterId) 
         + ", direction=" + std::to_string(direction) + ", launchForce=" + std::to_string(launchForce));
    DebugLog("Fire position: (" + std::to_string(fireX) + ", " + std::to_string(fireY) + ", " + std::to_string(fireZ) + ")");
    
    // 해당 클라이언트의 탱크 정보 가져오기
    if (tanks.find(remote) != tanks.end()) {
        TankInfo& tank = tanks[remote];
        DebugLog("Tank found: Position=(" + std::to_string(tank.posX) + "," + std::to_string(tank.posY) 
             + "), Direction=" + std::to_string(tank.direction));
        
        // 모든 클라이언트에게 총알 발사 정보 전송
        int recipientCount = 0;
        for (const auto& clientPair : tanks) {
            // 발사한 클라이언트 자신에게는 보내지 않음
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                
                DebugLog("Sending OnSpawnBullet to client " + std::to_string(static_cast<int>(clientPair.first)));
                tankProxy.OnSpawnBullet(clientPair.first, rmiCtx, (int)remote, shooterId, 
                                         tank.posX, tank.posY, direction, 
                                         launchForce, fireX, fireY, fireZ);
                recipientCount++;
            }
        }
        DebugLog("OnSpawnBullet sent to " + std::to_string(recipientCount) + " clients");
    } else {
        DebugLog("Error: Tank not found for client " + std::to_string(static_cast<int>(remote)));
    }
    DebugLog("========== SendFire Processing Completed ==========");
    
    return true;
}

// 탱크 타입 요청 처리
#ifdef _WIN32
DEFRMI_Tank_SendTankType(TankServer)
#else
bool TankServer::SendTankType(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const int& tankType)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("========== SendTankType Received ==========");
    DebugLog("From client " + std::to_string(static_cast<int>(remote)) + ": tankType=" + std::to_string(tankType));
    
    // 해당 클라이언트의 탱크 정보 업데이트
    if (tanks.find(remote) != tanks.end()) {
        tanks[remote].tankType = tankType;
        // DebugLog("Tank type updated for client " + std::to_string(static_cast<int>(remote)) + ": Type=" + std::to_string(tankType));
        
        // 모든 다른 클라이언트에게 이 클라이언트의 탱크 타입 알림
        for (const auto& clientPair : tanks) {
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                
                // OnPlayerJoined 메시지를 통해 탱크 타입 정보 전송
                // DebugLog("Notifying client " + std::to_string(static_cast<int>(clientPair.first)) + " about tank type of client " + std::to_string(static_cast<int>(remote)));
                tankProxy.OnPlayerJoined(clientPair.first, rmiCtx, (int)remote, 
                                         tanks[remote].posX, tanks[remote].posY, tankType);
            }
        }
    } else {
        DebugLog("Error: Tank not found for client " + std::to_string(static_cast<int>(remote)));
    }
    DebugLog("========== SendTankType Processing Completed ==========");
    
    return true;
}

// 체력 업데이트 처리
#ifdef _WIN32
DEFRMI_Tank_SendTankHealthUpdated(TankServer)
#else
bool TankServer::SendTankHealthUpdated(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const float& currentHealth, const float& maxHealth)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("========== SendTankHealthUpdated Received ==========");
    DebugLog("From client " + std::to_string(static_cast<int>(remote)) + ": currentHealth=" + std::to_string(currentHealth) 
         + ", maxHealth=" + std::to_string(maxHealth));
    
    // 해당 클라이언트의 탱크 정보 업데이트
    if (tanks.find(remote) != tanks.end()) {
        tanks[remote].currentHealth = currentHealth;
        tanks[remote].maxHealth = maxHealth;
        tanks[remote].isDestroyed = (currentHealth <= 0);
        
        DebugLog("Tank health updated for client " + std::to_string(static_cast<int>(remote)) + ": " 
             + std::to_string(currentHealth) + "/" + std::to_string(maxHealth));
        
        // 모든 다른 클라이언트에게 이 클라이언트의 체력 정보 전송
        for (const auto& clientPair : tanks) {
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                
                // DebugLog("Notifying client " + std::to_string(static_cast<int>(clientPair.first)) + " about health of client " + std::to_string(static_cast<int>(remote)));
                tankProxy.OnTankHealthUpdated(clientPair.first, rmiCtx, (int)remote, 
                                              currentHealth, maxHealth);
            }
        }
    } else {
        DebugLog("Error: Tank not found for client " + std::to_string(static_cast<int>(remote)));
    }
    DebugLog("========== SendTankHealthUpdated Processing Completed ==========");
    
    return true;
}

// 탱크 파괴 이벤트 처리
#ifdef _WIN32
DEFRMI_Tank_SendTankDestroyed(TankServer)
#else
bool TankServer::SendTankDestroyed(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const int& destroyedById)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("========== SendTankDestroyed Received ==========");
    DebugLog("From client " + std::to_string(static_cast<int>(remote)) + ": destroyedById=" + std::to_string(destroyedById));
    
    // 해당 클라이언트의 탱크 정보 업데이트
    if (tanks.find(remote) != tanks.end()) {
        tanks[remote].isDestroyed = true;
        tanks[remote].currentHealth = 0;
        
        string destroyedByText = destroyedById > 0 ? "by tank " + std::to_string(destroyedById) : "by environment";
        DebugLog("Tank destroyed for client " + std::to_string(static_cast<int>(remote)) + ": " + destroyedByText);
        
        // 모든 다른 클라이언트에게 이 클라이언트의 파괴 정보 전송
        for (const auto& clientPair : tanks) {
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                
                // DebugLog("Notifying client " + std::to_string(static_cast<int>(clientPair.first)) + " about destruction of client " + std::to_string(static_cast<int>(remote)));
                tankProxy.OnTankDestroyed(clientPair.first, rmiCtx, (int)remote, destroyedById);
            }
        }
    } else {
        DebugLog("Error: Tank not found for client " + std::to_string(static_cast<int>(remote)));
    }
    DebugLog("========== SendTankDestroyed Processing Completed ==========");
    
    return true;
}

// 탱크 생성/리스폰 메시지 처리
#ifdef _WIN32
DEFRMI_Tank_SendTankSpawned(TankServer)
#else
bool TankServer::SendTankSpawned(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const float& posX, const float& posY, const float& direction, const int& tankType, const float& initialHealth)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("========== SendTankSpawned Received ==========");
    DebugLog("From client " + std::to_string(static_cast<int>(remote)) + ": position=(" + std::to_string(posX) + "," + std::to_string(posY) 
         + "), direction=" + std::to_string(direction) + ", tankType=" + std::to_string(tankType) 
         + ", health=" + std::to_string(initialHealth));
    
    // 해당 클라이언트의 탱크 정보 업데이트
    if (tanks.find(remote) != tanks.end()) {
        tanks[remote].posX = posX;
        tanks[remote].posY = posY;
        tanks[remote].direction = direction;
        tanks[remote].tankType = tankType;
        tanks[remote].currentHealth = initialHealth;
        tanks[remote].maxHealth = initialHealth; // 최대 체력도 업데이트
        tanks[remote].isDestroyed = false;
        
        DebugLog("Tank spawned for client " + std::to_string(static_cast<int>(remote)) + " at (" + std::to_string(posX) + "," + std::to_string(posY) + ")");
        
        // 모든 다른 클라이언트에게 이 클라이언트의 생성/리스폰 정보 전송
        for (const auto& clientPair : tanks) {
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                
                // DebugLog("Notifying client " + std::to_string(static_cast<int>(clientPair.first)) + " about spawn of client " + std::to_string(static_cast<int>(remote)));
                tankProxy.OnTankSpawned(clientPair.first, rmiCtx, (int)remote, 
                                         posX, posY, direction, tankType, initialHealth);
            }
        }
    } else {
        DebugLog("Error: Tank not found for client " + std::to_string(static_cast<int>(remote)));
    }
    DebugLog("========== SendTankSpawned Processing Completed ==========");
    
    return true;
}

// P2P 메시지 처리
#ifdef _WIN32
DEFRMI_Tank_P2PMessage(TankServer)
#else
bool TankServer::P2PMessage(::Proud::HostID remote, ::Proud::RmiContext& rmiContext, const ::Proud::String& message)
#endif
{
    std::lock_guard<std::mutex> lock(mutex);
    
    // ProudNet::String을 C++ std::string으로 변환
    std::string messageStr = std::string(message);
    DebugLog("P2PMessage from client " + std::to_string(static_cast<int>(remote)) + ": " + messageStr);
    
    // P2P 그룹이 있는 경우 해당 클라이언트 제외한 모든 멤버에게 릴레이
    if (gameP2PGroupID != ::Proud::HostID_None && messageStr.find("P2P_GROUP_INFO:") == std::string::npos) {
        // 메시지 보낸 클라이언트를 제외한 모든 클라이언트에게 릴레이
        for (const auto& clientPair : tanks) {
            if (clientPair.first != remote) {
                ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                ::Proud::String relayedMessage;
                relayedMessage.Format(_PNT("RELAY_FROM_%d:%s"), static_cast<int>(remote), message.GetString());
                
                tankProxy.P2PMessage(clientPair.first, rmiCtx, relayedMessage);
                DebugLog("Relayed P2P message to client " + std::to_string(static_cast<int>(clientPair.first)) + ": " + std::string(relayedMessage));
            }
        }
    }
    
    return true;
}

// 서버 시작
void TankServer::Start() {
    Initialize();
    
    try {
        // 서버 시작 파라미터 설정
        ::Proud::CStartServerParameter serverParam;
        
        // Common 디렉토리의 Vars.h에 정의된 버전 정보 사용
        serverParam.m_protocolVersion = g_Version;
        serverParam.m_tcpPorts.Add(g_ServerPort);

        // WebSocket 설정
        serverParam.m_webSocketParam.webSocketType = WebSocket_Ws;
        serverParam.m_webSocketParam.listenPort = g_WebSocketPort;
        serverParam.m_webSocketParam.threadCount = 4;
        serverParam.m_webSocketParam.endpoint = _PNT("^/ws/?$");
        
        // 서버 시작
        server->Start(serverParam);
        
        DebugLog("========== Tank Server Started ==========");
        DebugLog("TCP Server listening on 0.0.0.0:" + std::to_string(g_ServerPort));
        DebugLog("WebSocket Server listening on 0.0.0.0:" + std::to_string(g_WebSocketPort) + "/ws");
        DebugLog("Ready to accept connections from all network interfaces");
        DebugLog("==========================================");
        
        // 커맨드 처리
        ProcessCommands();
    }
    catch (::Proud::Exception& e) {
        // what()으로 안전하게 오류 메시지 가져오기
        std::string errorMessage = std::string(e.what());
        DebugLog("Server start failed: " + errorMessage);
    }}

// 커맨드 처리 루프
void TankServer::ProcessCommands() {
    DebugLog("Server is running. Commands:");
    DebugLog("status: Show connected clients");
    DebugLog("health [id]: Show tank health (all or specific ID)");
    DebugLog("damage id amount: Apply damage to a tank");
    DebugLog("heal id amount: Heal a tank");
    DebugLog("respawn id x y: Respawn a tank at position (x,y)");
    DebugLog("q: Quit server");
    
    string input;
    while (true) {
        getline(cin, input);
        
        if (input == "q") {
            break;
        }
        else if (input == "status") {
            PrintConnectedClients();
        }
        else if (input.find("health") == 0) {
            ShowTankHealth(input);
        }
        else if (input.find("damage ") == 0) {
            ApplyDamageToTank(input);
        }
        else if (input.find("heal ") == 0) {
            HealTank(input);
        }
        else if (input.find("respawn ") == 0) {
            RespawnTank(input);
        }
    }
    
    // 서버 종료
    server->Stop();
    DebugLog("Server stopped");
}

// 연결된 클라이언트 정보 출력
void TankServer::PrintConnectedClients() {
    std::lock_guard<std::mutex> lock(mutex);
    
    DebugLog("========== Connected Clients ==========");
    DebugLog("Total: " + std::to_string(tanks.size()) + " clients");
    
    for (const auto& tank : tanks) {
        string healthStatus = tank.second.isDestroyed ? "DESTROYED" : 
                              std::to_string(tank.second.currentHealth) + "/" + std::to_string(tank.second.maxHealth);
        DebugLog("Client ID: " + std::to_string(static_cast<int>(tank.first)) + ", Position: (" + std::to_string(tank.second.posX) + "," + std::to_string(tank.second.posY) 
             + "), TankType: " + std::to_string(tank.second.tankType) + ", Health: " + healthStatus);
    }
    
    DebugLog("=======================================");
}

// 탱크 체력 정보 출력
void TankServer::ShowTankHealth(const string& input) {
    std::lock_guard<std::mutex> lock(mutex);
    
    std::istringstream iss(input);
    std::string cmd;
    int targetId = -1;
    
    iss >> cmd;
    iss >> targetId;
    
    if (targetId == -1) {
        // 모든 탱크의 체력 정보 출력
        DebugLog("========== Tank Health Status ==========");
        for (const auto& tank : tanks) {
            string healthStatus = tank.second.isDestroyed ? "DESTROYED" : 
                                   std::to_string(tank.second.currentHealth) + "/" + std::to_string(tank.second.maxHealth);
            DebugLog("Tank " + std::to_string(static_cast<int>(tank.first)) + ": " + healthStatus);
        }
        DebugLog("=======================================");
    } else {
        // 특정 탱크의 체력 정보 출력
        try {
            // 적절한 HostID 찾기
            ::Proud::HostID targetHostId = FindHostIDById(targetId);
            
            if (targetHostId != ::Proud::HostID_None && tanks.find(targetHostId) != tanks.end()) {
                const TankInfo& tank = tanks[targetHostId];
                string healthStatus = tank.isDestroyed ? "DESTROYED" : 
                                       std::to_string(tank.currentHealth) + "/" + std::to_string(tank.maxHealth);
                DebugLog("Tank " + std::to_string(targetId) + " health: " + healthStatus);
            } else {
                DebugLog("Tank with ID " + std::to_string(targetId) + " not found");
            }
        } catch (const std::exception& ex) {
            DebugLog("Invalid tank ID format: " + std::string(ex.what()));
        }
    }
}

// 탱크에 데미지 적용
void TankServer::ApplyDamageToTank(const string& input) {
    std::lock_guard<std::mutex> lock(mutex);
    
    std::istringstream iss(input);
    std::string cmd;
    int targetId = 0;
    float damageAmount = 0.0f;
    
    iss >> cmd >> targetId >> damageAmount;
    
    if (targetId > 0 && damageAmount > 0) {
        try {
            // 적절한 HostID 찾기
            ::Proud::HostID targetHostId = FindHostIDById(targetId);
            
            if (targetHostId != ::Proud::HostID_None && tanks.find(targetHostId) != tanks.end()) {
                TankInfo& tank = tanks[targetHostId];
                
                // 이미 파괴된 탱크라면 처리하지 않음
                if (tank.isDestroyed) {
                    DebugLog("Tank " + std::to_string(targetId) + " is already destroyed");
                    return;
                }
                
                // 체력 감소
                tank.currentHealth = std::max(0.0f, tank.currentHealth - damageAmount);
                
                // 파괴 여부 확인
                bool wasDestroyed = tank.currentHealth <= 0;
                tank.isDestroyed = wasDestroyed;
                
                // 클라이언트에게 체력 업데이트 전송
                for (const auto& clientPair : tanks) {
                    ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                    
                    tankProxy.OnTankHealthUpdated(clientPair.first, rmiCtx, targetId, 
                                                 tank.currentHealth, tank.maxHealth);
                    
                    // 파괴된 경우 파괴 이벤트도 전송
                    if (wasDestroyed) {
                        tankProxy.OnTankDestroyed(clientPair.first, rmiCtx, targetId, 0); // 서버에 의한 파괴는 0으로 표시
                    }
                }
                
                string result = wasDestroyed ? "DESTROYED" : 
                                std::to_string(tank.currentHealth) + "/" + std::to_string(tank.maxHealth);
                DebugLog("Applied " + std::to_string(damageAmount) + " damage to tank " + std::to_string(targetId) 
                     + ". New health: " + result);
            } else {
                DebugLog("Tank with ID " + std::to_string(targetId) + " not found");
            }
        } catch (const std::exception& ex) {
            DebugLog("Invalid parameters: " + std::string(ex.what()));
        }
    } else {
        DebugLog("Invalid parameters. Format: damage id amount");
    }
}

// 탱크 치유
void TankServer::HealTank(const string& input) {
    std::lock_guard<std::mutex> lock(mutex);
    
    std::istringstream iss(input);
    std::string cmd;
    int targetId = 0;
    float healAmount = 0.0f;
    
    iss >> cmd >> targetId >> healAmount;
    
    if (targetId > 0 && healAmount > 0) {
        try {
            // 적절한 HostID 찾기
            ::Proud::HostID targetHostId = FindHostIDById(targetId);
            
            if (targetHostId != ::Proud::HostID_None && tanks.find(targetHostId) != tanks.end()) {
                TankInfo& tank = tanks[targetHostId];
                
                // 이미 파괴된 탱크라면 처리하지 않음
                if (tank.isDestroyed) {
                    DebugLog("Tank " + std::to_string(targetId) + " is destroyed and cannot be healed");
                    return;
                }
                
                // 이미 최대 체력이라면 처리하지 않음
                if (tank.currentHealth >= tank.maxHealth) {
                    DebugLog("Tank " + std::to_string(targetId) + " already has full health");
                    return;
                }
                
                // 체력 회복 (최대 체력 초과하지 않도록)
                float oldHealth = tank.currentHealth;
                tank.currentHealth = std::min(tank.maxHealth, tank.currentHealth + healAmount);
                float actualHeal = tank.currentHealth - oldHealth;
                
                // 클라이언트에게 체력 업데이트 전송
                for (const auto& clientPair : tanks) {
                    ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                    
                    tankProxy.OnTankHealthUpdated(clientPair.first, rmiCtx, targetId, 
                                                 tank.currentHealth, tank.maxHealth);
                }
                
                DebugLog("Healed tank " + std::to_string(targetId) + " for " + std::to_string(actualHeal) 
                     + " points. New health: " + std::to_string(tank.currentHealth) + "/" + std::to_string(tank.maxHealth));
            } else {
                DebugLog("Tank with ID " + std::to_string(targetId) + " not found");
            }
        } catch (const std::exception& ex) {
            DebugLog("Invalid parameters: " + std::string(ex.what()));
        }
    } else {
        DebugLog("Invalid parameters. Format: heal id amount");
    }
}

// 탱크 리스폰
void TankServer::RespawnTank(const string& input) {
    std::lock_guard<std::mutex> lock(mutex);
    
    std::istringstream iss(input);
    std::string cmd;
    int targetId = 0;
    float posX = 0.0f;
    float posY = 0.0f;
    
    iss >> cmd >> targetId >> posX >> posY;
    
    if (targetId > 0) {
        try {
            // 적절한 HostID 찾기
            ::Proud::HostID targetHostId = FindHostIDById(targetId);
            
            if (targetHostId != ::Proud::HostID_None && tanks.find(targetHostId) != tanks.end()) {
                TankInfo& tank = tanks[targetHostId];
                
                // 탱크 정보 업데이트
                tank.posX = posX;
                tank.posY = posY;
                tank.currentHealth = tank.maxHealth; // 체력 회복
                tank.isDestroyed = false; // 파괴 상태 해제
                
                // 클라이언트에게 리스폰 정보 전송
                for (const auto& clientPair : tanks) {
                    ::Proud::RmiContext rmiCtx = CreateServerRmiContext();
                    
                    tankProxy.OnTankSpawned(clientPair.first, rmiCtx, targetId, posX, posY, 
                                           tank.direction, tank.tankType, tank.maxHealth);
                }
                
                DebugLog("Respawned tank " + std::to_string(targetId) + " at position (" + std::to_string(posX) + "," + std::to_string(posY) 
                     + ") with full health");
            } else {
                DebugLog("Tank with ID " + std::to_string(targetId) + " not found");
            }
        } catch (const std::exception& ex) {
            DebugLog("Invalid parameters: " + std::string(ex.what()));
        }
    } else {
        DebugLog("Invalid parameters. Format: respawn id x y");
    }
}

// 클라이언트 ID로 HostID 찾기
::Proud::HostID TankServer::FindHostIDById(int clientId) {
    for (const auto& tank : tanks) {
        if ((int)tank.first == clientId) {
            return tank.first;
        }
    }
    return ::Proud::HostID_None; // 찾지 못한 경우
}

// 메인 함수
int main() {
    srand(static_cast<unsigned int>(time(nullptr)));
    
    TankServer tankServer;
    tankServer.Start();
    
    return 0;
} 
