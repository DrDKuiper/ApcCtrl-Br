topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-alpha
all-uninstall: uninstall-alpha

install-alpha:
	$(call DISTINST,alpha)
	$(call MKDIR,/sbin/init.d)
	$(call INSTDATA,744,apcctrl,/sbin/init.d/apcctrl)
	$(call MKDIR,/sbin/rc2.d)
	$(call SYMLINK,../init.d/apcctrl,/sbin/rc2.d/K56apcctrl)
	$(call SYMLINK,../init.d/apcctrl,/sbin/rc2.d/S56apcctrl)
	$(call MKDIR,/sbin/rc3.d)
	$(call SYMLINK,../init.d/apcctrl,/sbin/rc3.d/K56apcctrl)
	$(call SYMLINK,../init.d/apcctrl,/sbin/rc3.d/S56apcctrl)
   # save old halt script
	$(call COPY,/sbin/rc0,/sbin/rc0.old)
   # insert apcctrl callout into halt script
	@echo "  AWK " $(DESTDIR)/sbin/rc0
	$(V)awk -f awkhaltprog $(DESTDIR)/sbin/rc0.old >$(DESTDIR)/sbin/rc0
	$(V)chmod 744 $(DESTDIR)/sbin/rc0
	@echo "================================================="
	@echo " "
	@echo "apcctrl script installation for the Alpha $(DISTVER) complete." 
	@echo " "
	@echo "You should now edit  /etc/apcctrl/apcctrl.conf  to correspond"
	@echo "to your setup then start the apcctrl daemon with:"
	@echo " "
	@echo "/sbin/init.d/apcctrl start"
	@echo " "
	@echo "thereafter when you reboot, it will be stopped and started"
	@echo "automatically."
	@echo " "
	@echo "Please check that your halt script in:"
	@echo "    /sbin/rc0"
	@echo "was properly updated (see installation section of manual)"
	@echo " "
	@echo "================================================="

uninstall-alpha:
	$(call DISTUNINST,alpha)
	-$(call UNINST,/sbin/rc2.d/K56apcctrl)
	-$(call UNINST,/sbin/rc2.d/S56apcctrl)
	-$(call UNINST,/sbin/rc3.d/K56apcctrl)
	-$(call UNINST,/sbin/rc3.d/S56apcctrl)
	-$(call UNINST,/sbin/init.d/apcctrl)
	$(call COPY,/sbin/rc0,/sbin/rc0.old)
	$(V)grep -v "###apcctrl###" $(DESTDIR)/sbin/rc0.old >$(DESTDIR)/sbin/rc0
	$(V)chmod 744 $(DESTDIR)/sbin/rc0
