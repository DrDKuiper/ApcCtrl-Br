// This file has been adapted to the Win32 version of apcctrl
// by Kern E. Sibbald.  Many thanks to ATT and James Weatherall,
// the original author, for providing an excellent template.
//
// Rewrite/Refactoring by Adam Kropelin
//
// Copyright (2007) Adam D. Kropelin
// Copyright (2000-2006) Kern E. Sibbald
//     20 July 2000

// System Headers
#include <unistd.h>
#include <windows.h>
#include <lmcons.h>
#include <ctype.h>
#include <signal.h>
#include <pthread.h>
#include <errno.h>

// apcctrl UNIX main entrypoint
extern int apcctrlMain(int argc, char **argv);

// Custom headers
#include "apcconfig.h"
#include "winups.h"
#include "winservice.h"
#include "compat.h"
#include "defines.h"
#include "winapi.h"

// Standard command-line flag definitions
char apcctrlRunService[] =        "/service";
char apcctrlRunAsUserApp[] =      "/run";
char apcctrlInstallService[] =    "/install";
char apcctrlRemoveService[] =     "/remove";
char apcctrlKillRunningCopy[] =   "/kill";
char apcctrlShowHelp[] =          "/help";
char apcctrlQuiet[] =             "/quiet";

// Usage string
static const char *apcctrlUsageText =
   "apcctrl [/quiet] [/run] [/kill] [/install] [/remove] [/help]\n";

// Application instance
static HINSTANCE hAppInstance;

// Command line argument storage
#define MAX_COMMAND_ARGS 100
static char apcctrl_name[] = "apcctrl";
static char *command_args[MAX_COMMAND_ARGS] = { apcctrl_name, NULL };
static int num_command_args = 1;
static char *winargs[MAX_COMMAND_ARGS];
static int num_winargs = 0;

// WinMain parses the command line and either calls the main App
// routine or, under NT, the main service routine.
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                   PSTR CmdLine, int iCmdShow)
{
   bool quiet = false;

   InitWinAPIWrapper();

   // Save the application instance and main thread id
   hAppInstance = hInstance;

   /* Build Unix style argc *argv[] */

   /* Don't NULL command_args[0] !!! */
   for (int i = 1; i < MAX_COMMAND_ARGS; i++)
      command_args[i] = NULL;

   // Split command line to windows and non-windows arguments
   char *arg;
   char *szCmdLine = CmdLine;
   while ((arg = GetArg(&szCmdLine))) {
      // Save the argument in appropriate list
      if (*arg != '/' && num_command_args < MAX_COMMAND_ARGS)
         command_args[num_command_args++] = arg;
      else if (num_winargs < MAX_COMMAND_ARGS)
         winargs[num_winargs++] = arg;
   }

   // Default Windows argument
   if (num_winargs == 0)
      winargs[num_winargs++] = apcctrlRunAsUserApp;

   // Act on Windows arguments...
   for (int i = 0; i < num_winargs; i++) {
      // /service
      if (strcasecmp(winargs[i], apcctrlRunService) == 0) {
         // Run apcctrl as a service
         return upsService::apcctrlServiceMain();
      }
      // /run  (this is the default if no command line arguments)
      if (strcasecmp(winargs[i], apcctrlRunAsUserApp) == 0) {
         // apcctrl is being run as a user-level program
         return apcctrlAppMain(0);
      }
      // /install
      if (strcasecmp(winargs[i], apcctrlInstallService) == 0) {
         // Install apcctrl as a service
         return upsService::InstallService(quiet);
      }
      // /remove
      if (strcasecmp(winargs[i], apcctrlRemoveService) == 0) {
         // Remove the apcctrl service
         return upsService::RemoveService(quiet);
      }
      // /kill
      if (strcasecmp(winargs[i], apcctrlKillRunningCopy) == 0) {
         // Kill any already running copy of apcctrl
         apcctrlTerminate();
         return 0;
      }
      // /quiet
      if (strcasecmp(winargs[i], apcctrlQuiet) == 0) {
         // Set quiet flag and go on to next argument
         quiet = true;
         continue;
      }
      // /help
      if (strcasecmp(winargs[i], apcctrlShowHelp) == 0) {
         MessageBox(NULL, apcctrlUsageText, "apcctrl Usage",
                    MB_OK | MB_ICONINFORMATION);
         return 0;
      }

      // Unknown option: Show the usage dialog
      MessageBox(NULL, winargs[i], "Bad Command Line Options", MB_OK);
      MessageBox(NULL, apcctrlUsageText, "apcctrl Usage",
                 MB_OK | MB_ICONINFORMATION);
      return 1;
   }
   return 0;
}

// Callback for processing Windows messages
static LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
   switch (uMsg)
   {
   // Clean exit requested
   case WM_DESTROY:
      PostQuitMessage(0);
      return 0;

   // Everything else uses default handling
   default:
      return DefWindowProc(hwnd, uMsg, wParam, lParam);
   }
}

static void PostToapcctrl(UINT message, WPARAM wParam, LPARAM lParam)
{
  // Locate the hidden apcctrl window
  HWND hservwnd = FindWindowEx(NULL, NULL, APCCTRL_WINDOW_CLASS, APCCTRL_WINDOW_NAME);
  if (hservwnd == NULL)
     return;

  // Post the message to apcctrl
  PostMessage(hservwnd, message, wParam, lParam);
}

void apcctrlTerminate()
{
   // Old versions of apcctrl and modern versions running on WinNT and
   // earlier need to receive a window message.
   PostToapcctrl(WM_CLOSE, 0, 0);

   // New apcctrl on Win2K and above listen for an event to be signaled.
   // This allows stopping apcctrl instances running under LocalSystem
   // and those started on other desktops.
   if (g_os_version >= WINDOWS_2000)
   {
      HANDLE evt = OpenEvent(EVENT_MODIFY_STATE, FALSE, APCCTRL_STOP_EVENT_NAME);
      if (evt != NULL)
      {
         SetEvent(evt);
         CloseHandle(evt);
      }
   }
}

// Called as a thread from apcctrlAppMain()
// Here we invoke apcctrl UNIX main loop
void *apcctrlMain(LPVOID lpwThreadParam)
{
   pthread_detach(pthread_self());

   // Call the "real" apcctrl
   apcctrlMain(num_command_args, command_args);

   // In case apcctrl returns, terminate application
   apcctrlTerminate();
   return NULL;
}

// This thread runs on Windows 2000 and higher. It monitors for the
// global exit event to be signaled (/kill).
bool runthread = false;
HANDLE exitevt = NULL;
DWORD WINAPI EventThread(LPVOID param)
{
   // Create global exit event and allow Adminstrator access to it so any
   // member of the Administrators group can signal it.
   exitevt = CreateEvent(NULL, TRUE, FALSE, APCCTRL_STOP_EVENT_NAME);
   TCHAR name[] = "Administrators";
   GrantAccess(exitevt, EVENT_MODIFY_STATE, TRUSTEE_IS_GROUP, name);

   // Wait for event to be signaled
   while (runthread)
   {
      DWORD rc = WaitForSingleObject(exitevt, INFINITE);
      if (!runthread || rc != WAIT_OBJECT_0)
         break;

      // Global exit event signaled
      runthread = false;
      PostToapcctrl(WM_CLOSE, 0, 0);
   }

   CloseHandle(exitevt);
   return 0;
}

// Wait for exit signal on WinNT and below. This code creates a hidden
// window and waits for a WM_CLOSE window message to be delivered. On
// modern platforms, we also monitor a global event.
static void WaitForExit()
{
   // Dummy window class
   WNDCLASSEX wndclass;
   wndclass.cbSize = sizeof(wndclass);
   wndclass.style = 0;
   wndclass.lpfnWndProc = WindowProc;
   wndclass.cbClsExtra = 0;
   wndclass.cbWndExtra = 0;
   wndclass.hInstance = hAppInstance;
   wndclass.hIcon = NULL;
   wndclass.hCursor = NULL;
   wndclass.hbrBackground = NULL;
   wndclass.lpszMenuName = NULL;
   wndclass.lpszClassName = APCCTRL_WINDOW_CLASS;
   wndclass.hIconSm = NULL;
   if (RegisterClassEx(&wndclass) == 0)
      return;

   // Create dummy window so we can receive Windows messages
   HWND hwnd = CreateWindow(APCCTRL_WINDOW_CLASS,  // class
                            APCCTRL_WINDOW_NAME,   // name/title
                            0,                     // style
                            0,                     // X pos
                            0,                     // Y pos
                            0,                     // width
                            0,                     // height
                            NULL,                  // parent
                            NULL,                  // menu
                            hAppInstance,          // app instance
                            NULL );                // create param
   if (hwnd == NULL)
      return;

   // On Win2K and above we spawn a thread to watch for exit requests.
   HANDLE evtthread = NULL;
   if (g_os_version >= WINDOWS_2000) {
      runthread = true;
      evtthread = CreateThread(NULL, 0, EventThread, NULL, 0, NULL);
   }

   // Now enter the Windows message handling loop until told to quit
   MSG msg;
   while (GetMessage(&msg, NULL, 0, 0))
   {
      TranslateMessage(&msg);
      DispatchMessage(&msg);
   }

   // Wait for event thread to exit cleanly
   if (g_os_version >= WINDOWS_2000) {
      runthread = false;
      SetEvent(exitevt); // Kick exitevt to wake up thread
      if (WaitForSingleObject(evtthread, 5000) == WAIT_TIMEOUT)
         TerminateThread(evtthread, 0);
      CloseHandle(evtthread);
   }

   DestroyWindow(hwnd);
}

// This is the main routine for apcctrl. It spawns a thread to run the
// UNIX apcctrl back end and waits to be told to exit.
int apcctrlAppMain(int service)
{
   // Set this process to be the last application to be shut down.
   SetProcessShutdownParameters(0x100, 0);

   // Check to see if we're already running
   HANDLE sem = CreateSemaphore(NULL, 0, 1, "apcctrl");
   if (sem == NULL || GetLastError() == ERROR_ALREADY_EXISTS) {
      MessageBox(NULL, "Another instance of apcctrl is already running", 
                 "apcctrl Error", MB_OK);
      return 0;
   }

   // Create a thread on which to run apcctrl UNIX main loop
   pthread_t tid;
   pthread_create(&tid, NULL, apcctrlMain, (void *)GetCurrentThreadId());

   // Wait for exit request.
   WaitForExit();

   pthread_kill(tid, SIGTERM);
   return 0;
}
