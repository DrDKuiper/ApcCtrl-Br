// This file has been adapted to the Win32 version of apcctrl
// by Kern E. Sibbald.  Many thanks to ATT and James Weatherall,
// the original author, for providing an excellent template.
//
// Rewrite/Refactoring by Adam Kropelin
//
// Copyright (2007) Adam D. Kropelin
// Copyright (2000) Kern E. Sibbald
//

#ifndef WINUPS_H
#define WINUPS_H

// WinUPS header file

#include <windows.h>

// Application specific messages
enum {
   // Message used for system tray notifications
   WM_APCTRAY_NOTIFY = WM_USER+1,

   // Message used to remove all apctray instances from the registry
   WM_APCTRAY_REMOVEALL,

   // Message used to remove specified apctray instance from the registry
   WM_APCTRAY_REMOVE,

   // Messages used to trigger redraw of tray icons
   WM_APCTRAY_RESET,

   // Message used to add a new apctray instance
   WM_APCTRAY_ADD
};

// apcctrl application window constants
#define APCCTRL_WINDOW_CLASS		"apcctrl"
#define APCCTRL_WINDOW_NAME		"apcctrl"

// apctray window constants
#define APCTRAY_WINDOW_CLASS		"apctray"
#define APCTRAY_WINDOW_NAME		"apctray"

// Command line option to start in service mode
extern char apcctrlRunService[];

// Names of various global events
#define APCCTRL_STOP_EVENT_NAME  "Global\\apcctrlStopEvent"
#define APCTRAY_STOP_EVENT_NAME  "Global\\ApctrayStopEvent"

// Main UPS server routine - Exported by winmain for use by winservice
extern int apcctrlAppMain(int service);

// Stop apcctrl - Exported by winmain for use by winservice
extern void apcctrlTerminate();

#endif // WINUPS_H
