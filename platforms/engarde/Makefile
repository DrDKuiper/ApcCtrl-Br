topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-engarde
all-uninstall: uninstall-engarde

install-engarde:
	$(call DISTINST,engarde)
   # unlink old rc script (if not doing a DESTDIR install)
	$(call CHKCFG,del,/etc/init.d/apcctrl)
   # install new rc script
	$(call MKDIR,/etc/init.d)
	$(call INSTDATA,744,apcctrl,/etc/init.d)
   # link new rc script (if not doing a DESTDIR install)
	$(call CHKCFG,add,/etc/init.d/apcctrl)
   # save old halt script
	$(call COPY,/etc/init.d/halt,/etc/init.d/halt.old)
   # insert apcctrl callout into halt script
	@echo "  AWK  " $(DESTDIR)/etc/init.d/halt
	$(V)awk -f awkhaltprog $(DESTDIR)/etc/init.d/halt.old >$(DESTDIR)/etc/init.d/halt
	$(V)chmod 744 $(DESTDIR)/etc/init.d/halt
	@echo "================================================="
	@echo " "
	@echo "apcctrl script installation for Engarde Secure Linux complete." 
	@echo " "
	@echo "You should now edit  /etc/apcctrl/apcctrl.conf  to correspond"
	@echo "to your setup then start the apcctrl daemon with:"
	@echo " "
	@echo "/etc/init.d/apcctrl start"
	@echo " "
	@echo "thereafter when you reboot, it will be stopped and started"
	@echo "automatically."
	@echo " "
	@echo "Please check that your halt script in:"
	@echo "    /etc/init.d/halt"
	@echo "was properly updated (see installation section of manual)"
	@echo " "
	@echo "================================================="

uninstall-engarde:
	$(call DISTUNINST,engarde)
   # unlink old rc script (if not doing a DESTDIR install)
	-$(call CHKCFG,del,/etc/init.d/apcctrl)
	-$(call UNINST,/etc/init.d/apcctrl)
	-$(call COPY,$(DESTDIR)/etc/init.d/halt.old,/etc/init.d/halt)
	-$(call UNINST,/etc/init.d/halt.old)

