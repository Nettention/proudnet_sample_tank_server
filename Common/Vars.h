#pragma once

#include "ProudNetCommon.h"

// Protocol version that you define.
// Your server app and client app must have the same value below.
extern Proud::Guid g_Version;

// TCP listening port number.
extern int g_ServerPort;

// WebSocket port number
extern int g_WebSocketPort;

// Web server port number
extern int g_WebServerPort; 