topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-mandrake
all-uninstall: uninstall-mandrake

install-mandrake:
	$(call DISTINST,Mandrake)
   # unlink old rc script (if not doing a DESTDIR install)
	$(call CHKCFG,del,/etc/rc.d/init.d/apcctrl)
   # install new rc script
	$(call MKDIR,/etc/rc.d/init.d)
	$(call INSTDATA,744,apcctrl,/etc/rc.d/init.d)
   # link new rc script (if not doing a DESTDIR install)
	$(call CHKCFG,add,/etc/rc.d/init.d/apcctrl)
   # mandrake halt script already handles apcctrl so no patching necessary
	@echo "================================================="
	@echo " "
	@echo "apcctrl script installation for Mandrake $(DISTVER) complete." 
	@echo " "
	@echo "You should now edit  /etc/apcctrl/apcctrl.conf  to correspond"
	@echo "to your setup then start the apcctrl daemon with:"
	@echo " "
	@echo "/etc/rc.d/init.d/apcctrl start"
	@echo " "
	@echo "thereafter when you reboot, it will be stopped and started"
	@echo "automatically."
	@echo " "
	@echo "================================================="

uninstall-mandrake:
	$(call DISTUNINST,Mandrake)
   # unlink old rc script (if not doing a DESTDIR install)
	-$(call CHKCFG,del,/etc/rc.d/init.d/apcctrl)
	-$(call UNINST,/etc/rc.d/init.d/apcctrl)
