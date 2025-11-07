topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-qnx
all-uninstall: uninstall-qnx

install-qnx:
	$(call DISTINST,QNX)
	@echo "You have to manually install apcctrl boot script and"
	@echo "halt script for clean emergency shutdown."
	@echo "Please contribute your distribution install to apcctrl team."
	@echo "I'm sorry: you are on your own."

uninstall-qnx:
	$(call DISTUNINST,QNX)
	@echo "You have to manually uninstall apcctrl boot script and"
	@echo "halt script."
	@echo "Please contribute your distribution install to apcctrl team."
	@echo "I'm sorry: you are on your own."
