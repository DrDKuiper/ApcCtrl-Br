topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-archlinux
all-uninstall: uninstall-archlinux

install-archlinux:
	$(call DISTINST,ArchLinux)
	$(call INSTDATA,555,apcctrl.service,/etc/systemd/system/apcctrl.service)
	$(call INSTPROG,555,apcctrl.sh,/usr/bin/apcctrl.sh)
	$(call MKDIR,$(LOCKDIR))
	systemctl enable apcctrl.service
	systemctl start apcctrl
	$(info ***** Instalado com sucesso *****)
	
uninstall-archlinux:
	$(call DISTUNINST,ArchLinux)
	systemctl stop apcctrl
	systemctl disable apcctrl.service
	-$(call UNINST,/etc/systemd/system/apcctrl.service)
	-$(call UNINST,/usr/bin/apcctrl.sh)
	$(info ***** É necessário remover a pasta /etc/apcctrl manualmente *****)
