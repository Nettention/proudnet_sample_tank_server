cmake_minimum_required(VERSION 3.10)
project(TankGameServer)

# Use C++17 standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# OpenSSL 라이브러리 검색 (Linux 전용)
if(NOT WIN32)
    # OpenSSL 검색
    find_package(OpenSSL)
    if(OPENSSL_FOUND)
        message(STATUS "OpenSSL found: ${OPENSSL_INCLUDE_DIR}")
        message(STATUS "OpenSSL libraries: ${OPENSSL_LIBRARIES}")
        message(STATUS "OpenSSL version: ${OPENSSL_VERSION}")
        
        # OpenSSL API 호환성 플래그 (한 번만 설정)
        add_definitions(-DOPENSSL_API_COMPAT=0x10000000L)
        add_definitions(-DOPENSSL_USE_DEPRECATED)
        add_definitions(-DOPENSSL_NO_DEPRECATED=0)  # 모순된 플래그 수정
    else()
        message(WARNING "OpenSSL not found on Linux. ProudNet requires OpenSSL for SSL/TLS support.")
    endif()
    
    # libcurl 검색
    find_package(CURL)
    if(CURL_FOUND)
        message(STATUS "CURL found: ${CURL_INCLUDE_DIRS}")
        message(STATUS "CURL libraries: ${CURL_LIBRARIES}")
        message(STATUS "CURL version: ${CURL_VERSION_STRING}")
    else()
        message(WARNING "CURL not found on Linux. ProudNet requires CURL for HTTP support.")
    endif()
else()
    # Windows에서는 ProudNet이 이미 OpenSSL과 CURL을 포함하고 있음
    # ProudNet DLL과 함께 배포된 libcrypto-1_1-x64.dll 및 libssl-1_1-x64.dll 사용
    message(STATUS "Building on Windows - Using OpenSSL and CURL from ProudNet")
endif()

# 디버깅을 위한 빌드 정보 출력 추가
message(STATUS "시스템 확인: ${CMAKE_SYSTEM_NAME}")
message(STATUS "현재 작업 디렉토리: ${CMAKE_CURRENT_SOURCE_DIR}")

# Set additional compiler flags
if(MSVC)
    # Use UTF-8 encoding for source files
    add_compile_options(/utf-8)
    # Disable specific warnings
    add_compile_options(/wd4819 /wd4996 /wd4267 /wd4244 /wd4018)
    # 추가 정의 사항
    add_definitions(-D_WINSOCK_DEPRECATED_NO_WARNINGS)
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    add_definitions(-DNOMINMAX)
endif()

# Source files
set(SOURCES
    src/TankServer.cpp
    ../Common/Vars.cpp
)

# ProudNet installation path (modify according to your environment)
if(DEFINED ENV{PROUDNET_PATH})
    set(PROUDNET_PATH $ENV{PROUDNET_PATH})
else()
    # 윈도우 환경
    if(WIN32)
        set(PROUDNET_PATH "C:/Users/wayne/Documents/proud_tank/ProudNet")
    # 리눅스 환경
    else()
        set(PROUDNET_PATH "/app/ProudNet")
    endif()
endif()

# Define executable
add_executable(TankGameServer ${SOURCES})
set_target_properties(TankGameServer PROPERTIES OUTPUT_NAME "TankServer")

# Include header directories
target_include_directories(TankGameServer PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${CMAKE_CURRENT_SOURCE_DIR}/../Common
    ${PROUDNET_PATH}/include
)

# Linux 환경에서만 OpenSSL 헤더 추가
if(NOT WIN32 AND OPENSSL_FOUND)
    set(OPENSSL_INCLUDE_DIR_VARIABLE ${OPENSSL_INCLUDE_DIR})
    target_include_directories(TankGameServer PRIVATE ${OPENSSL_INCLUDE_DIR_VARIABLE})
endif()

# Linux 환경에서만 CURL 헤더 추가
if(NOT WIN32 AND CURL_FOUND)
    target_include_directories(TankGameServer PRIVATE ${CURL_INCLUDE_DIRS})
endif()

# 디버깅을 위한 빌드 정보 출력 추가
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "ProudNet Path: ${PROUDNET_PATH}")
message(STATUS "Operating System: ${CMAKE_SYSTEM_NAME}")

# 시스템에 따라 다른 라이브러리 설정
if(WIN32)
    # Visual Studio 경로 및 ATL 라이브러리 경로 설정
    if(MSVC)
        # Visual Studio 설치 경로 자동 탐지
        get_filename_component(VS_PATH "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\14.0\\Setup\\VS;ProductDir]" ABSOLUTE)
        if(NOT VS_PATH)
            # 기본 Visual Studio 2019+ 경로 시도
            set(VS_PATH "C:/Program Files (x86)/Microsoft Visual Studio")
            message(STATUS "Visual Studio Registry Key not found. Using default path: ${VS_PATH}")
        endif()
        
        # ATL 라이브러리 검색
        file(GLOB_RECURSE ATL_LIB_PATH 
            "${VS_PATH}/*/atlmfc/lib/x64/atls.lib"
            "C:/Program Files*/Microsoft Visual Studio/*/Community/VC/Tools/MSVC/*/atlmfc/lib/x64/atls.lib"
            "C:/Program Files*/Microsoft Visual Studio/*/Professional/VC/Tools/MSVC/*/atlmfc/lib/x64/atls.lib"
            "C:/Program Files*/Microsoft Visual Studio/*/Enterprise/VC/Tools/MSVC/*/atlmfc/lib/x64/atls.lib"
        )
        
        if(ATL_LIB_PATH)
            message(STATUS "Found ATL Library: ${ATL_LIB_PATH}")
            get_filename_component(ATL_LIB_DIR ${ATL_LIB_PATH} DIRECTORY)
            link_directories(${ATL_LIB_DIR})
        else()
            message(WARNING "ATL Library (atls.lib) not found. You may need to install ATL/MFC support in Visual Studio.")
        endif()
    endif()

    # Windows 환경에서의 라이브러리 경로 처리
    if(CMAKE_BUILD_TYPE STREQUAL "Debug")
        # Debug 빌드용 라이브러리 디렉토리
        link_directories(
            ${PROUDNET_PATH}/lib/x64/v140/Debug
        )
        
        # 체크를 위한 전체 경로 출력
        message(STATUS "Using Debug Library Path: ${PROUDNET_PATH}/lib/x64/v140/Debug")
        
        # Debug 라이브러리
        set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x64/v140/Debug/ProudNetServer.lib")
        set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x64/v140/Debug/ProudNetClient.lib")
    else()
        # Release 빌드용 라이브러리 디렉토리
        link_directories(
            ${PROUDNET_PATH}/lib/x64/v140/Release
        )
        
        # 체크를 위한 전체 경로 출력
        message(STATUS "Using Release Library Path: ${PROUDNET_PATH}/lib/x64/v140/Release")
        
        # Release 라이브러리
        set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x64/v140/Release/ProudNetServer.lib")
        set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x64/v140/Release/ProudNetClient.lib")
    endif()

    # Windows 전용 링크 라이브러리
    target_link_libraries(TankGameServer
        ${PROUDNET_SERVER_LIB}
        ${PROUDNET_CLIENT_LIB}
        ws2_32     # Windows socket library
        winmm      # Windows multimedia library
        iphlpapi   # IP Helper API
        atls       # ATL 라이브러리 추가
    )
else()
    # Linux 환경에서의 라이브러리 경로 처리
    # 라이브러리 경로 존재 확인
    message(STATUS "라이브러리 경로 확인: ${PROUDNET_PATH}/lib/x86_x64-linux")
    
    # 경로가 존재하는지 확인
    if(EXISTS "${PROUDNET_PATH}/lib/x86_x64-linux")
        message(STATUS "Found Linux ProudNet library: ${PROUDNET_PATH}/lib/x86_x64-linux")
        
        # 추가 C++ 플래그 설정
        set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -pthread")
        
        # ProudNet에서 OpenSSL 호출 비활성화 제거 (SSL을 명시적으로 사용)
        # add_definitions(-DNO_OPENSSL)
        # add_definitions(-DPROUDNET_DISABLE_SSL)
        
        # 디렉토리 내용 확인
        file(GLOB LIB_FILES "${PROUDNET_PATH}/lib/x86_x64-linux/*")
        message(STATUS "라이브러리 파일 목록: ${LIB_FILES}")
        
        # Linux 라이브러리 디렉토리 설정
        link_directories(
            ${PROUDNET_PATH}/lib/x86_x64-linux/Debug
            ${PROUDNET_PATH}/lib/x86_x64-linux/Release
        )
        
        # Linux용 ProudNet 라이브러리 - 정적 라이브러리 전체 경로 지정
        if(CMAKE_BUILD_TYPE STREQUAL "Debug")
            set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Debug/libProudNetServer.a")
            set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Debug/libProudNetClient.a")
        else()
            set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Release/libProudNetServer.a")
            set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Release/libProudNetClient.a")
        endif()
        
        # 링크 순서: 시스템 라이브러리 -> ODBC -> OpenSSL -> CURL -> ProudNet
        
        # 1. 일반 시스템 라이브러리 링크
        target_link_libraries(TankGameServer
            pthread
            dl
            rt
        )
        
        # 2. ODBC 라이브러리 링크
        find_library(ODBC_LIBRARY 
            NAMES odbc libodbc odbc32
            PATHS /lib /usr/lib /usr/local/lib /lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
            DOC "ODBC library")
        
        find_library(ODBCINST_LIBRARY 
            NAMES odbcinst libodbcinst odbcinst32
            PATHS /lib /usr/lib /usr/local/lib /lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
            DOC "ODBC Installer library")
        
        if(ODBC_LIBRARY)
            message(STATUS "Found ODBC library: ${ODBC_LIBRARY}")
            target_link_libraries(TankGameServer ${ODBC_LIBRARY})
        else()
            message(WARNING "ODBC library not found, trying generic name")
            target_link_libraries(TankGameServer odbc)
        endif()
        
        if(ODBCINST_LIBRARY)
            message(STATUS "Found ODBCINST library: ${ODBCINST_LIBRARY}")
            target_link_libraries(TankGameServer ${ODBCINST_LIBRARY})
        else()
            message(WARNING "ODBCINST library not found, trying generic name")
            target_link_libraries(TankGameServer odbcinst)
        endif()
        
        # 3. OpenSSL 라이브러리 명시적 링크 
        if(OPENSSL_FOUND)
            message(STATUS "Linking OpenSSL libraries: ${OPENSSL_LIBRARIES}")
            target_link_libraries(TankGameServer ${OPENSSL_LIBRARIES})
        else()
            # 기본 OpenSSL 라이브러리 직접 지정 시도
            message(STATUS "OpenSSL not found by find_package, trying direct link")
            find_library(SSL_LIBRARY 
                NAMES ssl libssl
                PATHS /lib /usr/lib /usr/local/lib /lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
                DOC "SSL library")
            
            find_library(CRYPTO_LIBRARY 
                NAMES crypto libcrypto
                PATHS /lib /usr/lib /usr/local/lib /lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
                DOC "Crypto library")
            
            if(SSL_LIBRARY)
                message(STATUS "Found SSL library: ${SSL_LIBRARY}")
                target_link_libraries(TankGameServer ${SSL_LIBRARY})
            else()
                target_link_libraries(TankGameServer ssl)
            endif()
            
            if(CRYPTO_LIBRARY)
                message(STATUS "Found Crypto library: ${CRYPTO_LIBRARY}")
                target_link_libraries(TankGameServer ${CRYPTO_LIBRARY})
            else()
                target_link_libraries(TankGameServer crypto)
            endif()
        endif()
        
        # 4. CURL 라이브러리 명시적 링크
        if(CURL_FOUND)
            message(STATUS "Linking CURL library: ${CURL_LIBRARIES}")
            target_link_libraries(TankGameServer ${CURL_LIBRARIES})
        else()
            find_library(CURL_LIBRARY 
                NAMES curl libcurl
                PATHS /lib /usr/lib /usr/local/lib /lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
                DOC "CURL library")
            
            if(CURL_LIBRARY)
                message(STATUS "Found CURL library: ${CURL_LIBRARY}")
                target_link_libraries(TankGameServer ${CURL_LIBRARY})
            else()
                message(WARNING "CURL library not found, trying generic name")
                target_link_libraries(TankGameServer curl)
            endif()
        endif()
        
        # 5. ProudNet 라이브러리를 마지막에 링크 (다른 라이브러리 의존성 해결)
        target_link_libraries(TankGameServer
            ${PROUDNET_SERVER_LIB}
            ${PROUDNET_CLIENT_LIB}
        )
        
        # 링크 문제 해결을 위한 추가 플래그
        set_target_properties(TankGameServer PROPERTIES LINK_FLAGS "-Wl,--allow-multiple-definition -Wl,--no-as-needed")
        
        # 누락됐을 수 있는 라이브러리 추가 시도
        if(NOT TARGET Threads::Threads)
            find_package(Threads REQUIRED)
            target_link_libraries(TankGameServer Threads::Threads)
        endif()
        
    else()
        message(WARNING "Linux ProudNet library not found at: ${PROUDNET_PATH}/lib/x86_x64-linux. Will try to build without ProudNet libraries.")
        
        # 대체 방안: 라이브러리 없이 빌드 시도
        add_definitions(-DWITHOUT_PROUDNET)
        
        # Linux 기본 라이브러리만 링크
        target_link_libraries(TankGameServer
            pthread
            dl
            rt
        )
    endif()
endif() 