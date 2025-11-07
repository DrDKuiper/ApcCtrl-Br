topdir:=..

SUBDIRS = lib drivers $(CGIDIR) $(USBHIDDIR) $(GAPCMON) $(APCAGENT) $(WIN32)
include $(topdir)/autoconf/targets.mak

# Force dirs that needs libapc.a to build after lib_DIR
# Necessary for parallel make since they link against libapc.a
cgi_DIR: lib_DIR
apcagent_DIR: lib_DIR
win32_DIR: lib_DIR

# CGI requires win32, but only if building for win32
cgi_DIR: $(if $(WIN32),win32_DIR)

common_srcs     := options.c device.c reports.c action.c
apcctrl_srcs    := apcctrl.c apcnis.c
apcaccess_srcs  := apcaccess.c
apctest_srcs    := apctest.c

common_obj      := $(call SRC2OBJ,$(common_srcs))
apcctrl_obj     := $(call SRC2OBJ,$(apcctrl_srcs)) $(if $(WIN32),$(call SRC2OBJ,win32/winservice.cpp win32/winmain.cpp))
apcaccess_obj   := $(call SRC2OBJ,$(apcaccess_srcs))
apctest_obj     := $(call SRC2OBJ,$(apctest_srcs))

SRCS = $(common_srcs) $(apcctrl_srcs) $(apcaccess_srcs) $(apctest_srcs) 

all-targets: apcctrl$(EXE) apcaccess$(EXE) apctest$(EXE) 

apcctrl$(EXE): $(common_obj) $(apcctrl_obj) $(APCDRVLIBS) $(APCLIBS)
	$(LINK) $(DRVLIBS) $(BG)

apctest$(EXE): $(common_obj) $(apctest_obj) $(APCDRVLIBS) $(APCLIBS)
	$(LINK) $(DRVLIBS)

apcaccess$(EXE): $(apcaccess_obj) $(APCLIBS)
	$(LINK)

all-install: install-base
all-uninstall: uninstall-base

install-base:
	$(call MKDIR,$(sbindir))
	$(call INSTPROG,700,apcctrl,$(sbindir)/apcctrl$(EXE))
	$(call INSTPROG,700,apctest,$(sbindir)/apctest$(EXE))
	$(call INSTPROG,755,apcaccess,$(sbindir)/apcaccess$(EXE))

uninstall-base:
	$(call UNINST,$(sbindir)/apcctrl$(EXE))
	$(call UNINST,$(sbindir)/apctest$(EXE))
	$(call UNINST,$(sbindir)/apcaccess$(EXE))

# Include dependencies
-include $(DEPS)
