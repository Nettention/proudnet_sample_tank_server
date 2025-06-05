cmake_minimum_required(VERSION 3.10)
project(TankGameServer)

# Use C++17 standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# OpenSSL library search (Linux only)
if(NOT WIN32)
    # OpenSSL search
    find_package(OpenSSL)
    if(OPENSSL_FOUND)
        message(STATUS "OpenSSL found: ${OPENSSL_INCLUDE_DIR}")
        message(STATUS "OpenSSL libraries: ${OPENSSL_LIBRARIES}")
        message(STATUS "OpenSSL version: ${OPENSSL_VERSION}")
        
        # OpenSSL API compatibility flag (set only once)
        add_definitions(-DOPENSSL_API_COMPAT=0x10000000L)
        add_definitions(-DOPENSSL_USE_DEPRECATED)
        add_definitions(-DOPENSSL_NO_DEPRECATED=0)  # Fix conflicting flag
    else()
        message(WARNING "OpenSSL not found on Linux. ProudNet requires OpenSSL for SSL/TLS support.")
    endif()
    
    # libcurl search
    find_package(CURL)
    if(CURL_FOUND)
        message(STATUS "CURL found: ${CURL_INCLUDE_DIRS}")
        message(STATUS "CURL libraries: ${CURL_LIBRARIES}")
        message(STATUS "CURL version: ${CURL_VERSION_STRING}")
    else()
        message(WARNING "CURL not found on Linux. ProudNet requires CURL for HTTP support.")
    endif()
else()
    # On Windows, ProudNet already includes OpenSSL and CURL
    # Using libcrypto-1_1-x64.dll and libssl-1_1-x64.dll distributed with ProudNet DLL
    message(STATUS "Building on Windows - Using OpenSSL and CURL from ProudNet")
endif()

# Add debugging build information output
message(STATUS "System check: ${CMAKE_SYSTEM_NAME}")
message(STATUS "Current working directory: ${CMAKE_CURRENT_SOURCE_DIR}")

# Set additional compiler flags
if(MSVC)
    # Use UTF-8 encoding for source files
    add_compile_options(/utf-8)
    # Disable specific warnings
    add_compile_options(/wd4819 /wd4996 /wd4267 /wd4244 /wd4018)
    # Additional definitions
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
    # Check if running in Docker environment
    if(EXISTS "/app/ProudNet")
        # Docker environment - ProudNet is copied to /app/ProudNet
        set(PROUDNET_PATH "/app/ProudNet")
    else()
        # Windows environment - relative path usage
        if(WIN32)
            set(PROUDNET_PATH "../../ProudNet")
        # Linux environment - relative path usage
        else()
            set(PROUDNET_PATH "../../ProudNet")
        endif()
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

# Linux environment only OpenSSL header addition
if(NOT WIN32 AND OPENSSL_FOUND)
    set(OPENSSL_INCLUDE_DIR_VARIABLE ${OPENSSL_INCLUDE_DIR})
    target_include_directories(TankGameServer PRIVATE ${OPENSSL_INCLUDE_DIR_VARIABLE})
endif()

# Linux environment only CURL header addition
if(NOT WIN32 AND CURL_FOUND)
    target_include_directories(TankGameServer PRIVATE ${CURL_INCLUDE_DIRS})
endif()

# Add debugging build information output
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "ProudNet Path: ${PROUDNET_PATH}")
message(STATUS "Operating System: ${CMAKE_SYSTEM_NAME}")

# Different library settings based on system
if(WIN32)
    # Visual Studio path and ATL library path settings
    if(MSVC)
        # Visual Studio version information check
        set(VS_VERSION_VALUE "2022")
        if(DEFINED VS_VERSION)
            set(VS_VERSION_VALUE ${VS_VERSION})
            message(STATUS "Using Visual Studio version from command line: ${VS_VERSION_VALUE}")
        else()
            # Try to automatically detect Visual Studio version
            if(MSVC_VERSION GREATER_EQUAL 1930)
                set(VS_VERSION_VALUE "2022")
            elseif(MSVC_VERSION GREATER_EQUAL 1920)
                set(VS_VERSION_VALUE "2019")
            elseif(MSVC_VERSION GREATER_EQUAL 1910)
                set(VS_VERSION_VALUE "2017")
            elseif(MSVC_VERSION GREATER_EQUAL 1900)
                set(VS_VERSION_VALUE "2015")
            else()
                set(VS_VERSION_VALUE "Unknown")
            endif()
            message(STATUS "Detected Visual Studio version: ${VS_VERSION_VALUE}")
        endif()

        # Automatically detect Visual Studio installation path (adjust based on version)
        if(VS_VERSION_VALUE STREQUAL "2022")
            set(VS_PATH_BASE "C:/Program Files/Microsoft Visual Studio")
            set(VS_PATH_EDITION_LIST "Community;Professional;Enterprise")
            set(VS_PATH_YEAR "2022")
        elseif(VS_VERSION_VALUE STREQUAL "2019")
            set(VS_PATH_BASE "C:/Program Files (x86)/Microsoft Visual Studio")
            set(VS_PATH_EDITION_LIST "Community;Professional;Enterprise;BuildTools")
            set(VS_PATH_YEAR "2019")
        elseif(VS_VERSION_VALUE STREQUAL "2017")
            set(VS_PATH_BASE "C:/Program Files (x86)/Microsoft Visual Studio")
            set(VS_PATH_EDITION_LIST "Community;Professional;Enterprise;BuildTools")
            set(VS_PATH_YEAR "2017")
        else()
            set(VS_PATH_BASE "C:/Program Files (x86)/Microsoft Visual Studio 14.0")
            set(VS_PATH_EDITION_LIST "")
            set(VS_PATH_YEAR "")
        endif()
        
        set(VS_PATH "")
        if(VS_PATH_EDITION_LIST)
            # Find existing edition
            foreach(EDITION ${VS_PATH_EDITION_LIST})
                set(TEST_PATH "${VS_PATH_BASE}/${VS_PATH_YEAR}/${EDITION}")
                if(EXISTS "${TEST_PATH}")
                    set(VS_PATH "${TEST_PATH}")
                    message(STATUS "Found Visual Studio installation: ${VS_PATH}")
                    break()
                endif()
            endforeach()
        else()
            # VS 2015 or earlier
            if(EXISTS "${VS_PATH_BASE}")
                set(VS_PATH "${VS_PATH_BASE}")
            endif()
        endif()
        
        if(NOT VS_PATH)
            message(WARNING "Visual Studio installation not found. Using default paths.")
        else()
            message(STATUS "Visual Studio Path: ${VS_PATH}")
        endif()
    endif()

    # Windows environment library path handling
    if(CMAKE_BUILD_TYPE STREQUAL "Debug")
        # Debug build library directory
        link_directories(
            ${PROUDNET_PATH}/lib/x64/v140/Debug
        )
        
        # Full path output for checking
        message(STATUS "Using Debug Library Path: ${PROUDNET_PATH}/lib/x64/v140/Debug")
        
        # Debug library
        set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x64/v140/Debug/ProudNetServer.lib")
        set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x64/v140/Debug/ProudNetClient.lib")
    else()
        # Release build library directory
        link_directories(
            ${PROUDNET_PATH}/lib/x64/v140/Release
        )
        
        # Full path output for checking
        message(STATUS "Using Release Library Path: ${PROUDNET_PATH}/lib/x64/v140/Release")
        
        # Release library
        set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x64/v140/Release/ProudNetServer.lib")
        set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x64/v140/Release/ProudNetClient.lib")
    endif()

    # Windows specific link libraries
    target_link_libraries(TankGameServer
        ${PROUDNET_SERVER_LIB}
        ${PROUDNET_CLIENT_LIB}
        ws2_32     # Windows socket library
        winmm      # Windows multimedia library
        iphlpapi   # IP Helper API
        atls       # ATL library addition
    )
else()
    # Linux environment library path handling
    # Check existence of library path
    message(STATUS "Library path check: ${PROUDNET_PATH}/lib/x86_x64-linux")
    
    # Check if path exists
    if(EXISTS "${PROUDNET_PATH}/lib/x86_x64-linux")
        message(STATUS "Found Linux ProudNet library: ${PROUDNET_PATH}/lib/x86_x64-linux")
        
        # Additional C++ flag settings
        set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -pthread")
        
        # ProudNet OpenSSL call deactivation removal (explicit SSL use)
        # add_definitions(-DNO_OPENSSL)
        # add_definitions(-DPROUDNET_DISABLE_SSL)
        
        # Directory content check
        file(GLOB LIB_FILES "${PROUDNET_PATH}/lib/x86_x64-linux/*")
        message(STATUS "Library file list: ${LIB_FILES}")
        
        # Linux library directory settings
        link_directories(
            ${PROUDNET_PATH}/lib/x86_x64-linux/Debug
            ${PROUDNET_PATH}/lib/x86_x64-linux/Release
        )
        
        # Linux ProudNet library for static library - Full path specification
        if(CMAKE_BUILD_TYPE STREQUAL "Debug")
            set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Debug/libProudNetServer.a")
            set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Debug/libProudNetClient.a")
        else()
            set(PROUDNET_SERVER_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Release/libProudNetServer.a")
            set(PROUDNET_CLIENT_LIB "${PROUDNET_PATH}/lib/x86_x64-linux/Release/libProudNetClient.a")
        endif()
        
        # Link order: System library -> ODBC -> OpenSSL -> CURL -> ProudNet
        
        # 1. General system library link
        target_link_libraries(TankGameServer
            pthread
            dl
            rt
        )
        
        # 2. ODBC library link
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
        
        # 3. Explicit OpenSSL library link 
        if(OPENSSL_FOUND)
            message(STATUS "Linking OpenSSL libraries: ${OPENSSL_LIBRARIES}")
            target_link_libraries(TankGameServer ${OPENSSL_LIBRARIES})
        else()
            # Direct OpenSSL library specification attempt
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
        
        # 4. Explicit CURL library link
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
        
        # 5. ProudNet library linking at the end (resolving other library dependencies)
        target_link_libraries(TankGameServer
            ${PROUDNET_SERVER_LIB}
            ${PROUDNET_CLIENT_LIB}
        )
        
        # Link issue resolution additional flags
        set_target_properties(TankGameServer PROPERTIES LINK_FLAGS "-Wl,--allow-multiple-definition -Wl,--no-as-needed")
        
        # Missing library addition attempt
        if(NOT TARGET Threads::Threads)
            find_package(Threads REQUIRED)
            target_link_libraries(TankGameServer Threads::Threads)
        endif()
        
    else()
        message(WARNING "Linux ProudNet library not found at: ${PROUDNET_PATH}/lib/x86_x64-linux. Will try to build without ProudNet libraries.")
        
        # Alternative option: Try to build without libraries
        add_definitions(-DWITHOUT_PROUDNET)
        
        # Linux basic library linking
        target_link_libraries(TankGameServer
            pthread
            dl
            rt
        )
    endif()
endif() 