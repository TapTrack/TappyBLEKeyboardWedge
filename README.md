# TappyKeyboardWedge
Use the TappyBLE as a keyboard entry device while maintaining an active heartbeat with the reader.  

## Required TappyBLE firmware configuration

The TappyBLE can feature a special firmware configuration known as kiosk mode (or keyboard wedge mode). This mode is not available in the TappyUSB model. When configured in this special mode, the TappyBLE is able to maintain an active heartbeat with its host, which in this instance is a Windows utility using a BLE dongle from Bluegiga model BLED112.  The heartbeat helps overcome two constraints:
 
1. When the operator will not regularly have the PC display visible do notice if the BLE connection drops.  Under normal configuration, the TappyBLE will continue to scan even if the BLE connection is lost.  It will only stop scanning if a timeout was set, otherwise the operator may continue scanning without knowledge that the entries are not being made.
 
2. The undefined amount of time that passes between the BLE connection failing and the event being raised on the host.  On mobile platforms such as Android this may be up to a minute or sometimes longer. By maintaining an active heartbeat with the host, the TappyBLE in kiosk mode can have its BLE connection diagnosed and recovered in a more reliable and expedient fashion. 

## Required Bluetooth Low Energy Adaptor Dongle

Due to inconsistency among Bluetooth adaptors on various PCs as well as limited driver support in Windows, this utility uses a USB Bluetooth Low Energy dongle from Bluegiga, model [BLED112](http://www.silabs.com/products/wireless/bluetooth/bluetooth-low-energy-modules/bled112-bluetooth-smart-dongle). One must be installed in the PC before the TappyKeyboardWedge utility will connect to any TappyBLE readers. 

