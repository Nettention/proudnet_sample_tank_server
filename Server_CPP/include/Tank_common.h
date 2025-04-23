#pragma once

namespace Tank {


	//Message ID that replies to each RMI method. 
               
    static const ::Proud::RmiID Rmi_SendMove = (::Proud::RmiID)(2000+1);
               
    static const ::Proud::RmiID Rmi_SendFire = (::Proud::RmiID)(2000+2);
               
    static const ::Proud::RmiID Rmi_SendTankType = (::Proud::RmiID)(2000+3);
               
    static const ::Proud::RmiID Rmi_SendTankHealthUpdated = (::Proud::RmiID)(2000+4);
               
    static const ::Proud::RmiID Rmi_SendTankDestroyed = (::Proud::RmiID)(2000+5);
               
    static const ::Proud::RmiID Rmi_SendTankSpawned = (::Proud::RmiID)(2000+6);
               
    static const ::Proud::RmiID Rmi_OnPlayerJoined = (::Proud::RmiID)(2000+7);
               
    static const ::Proud::RmiID Rmi_OnPlayerLeft = (::Proud::RmiID)(2000+8);
               
    static const ::Proud::RmiID Rmi_OnTankPositionUpdated = (::Proud::RmiID)(2000+9);
               
    static const ::Proud::RmiID Rmi_OnTankHealthUpdated = (::Proud::RmiID)(2000+10);
               
    static const ::Proud::RmiID Rmi_OnTankDestroyed = (::Proud::RmiID)(2000+11);
               
    static const ::Proud::RmiID Rmi_OnTankSpawned = (::Proud::RmiID)(2000+12);
               
    static const ::Proud::RmiID Rmi_OnSpawnBullet = (::Proud::RmiID)(2000+13);
               
    static const ::Proud::RmiID Rmi_P2PMessage = (::Proud::RmiID)(2000+14);

	// List that has RMI ID.
	extern ::Proud::RmiID g_RmiIDList[];
	// RmiID List Count
	extern int g_RmiIDListCount;

}


 

// Forward declarations


// Declarations



