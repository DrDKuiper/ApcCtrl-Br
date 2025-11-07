topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-openbsd
all-uninstall: uninstall-openbsd

install-openbsd:
	$(call DISTINST,OpenBSD)
	$(call MKDIR,/etc)
	$(call INSTPROG,744,apcctrl,/etc/rc.apcctrl)
	$(VV)-today="`date +%Y%m%d%H%M`"; \
	grep -q '# TAG_APCCTRL' /etc/rc.shutdown; \
	if [ $$? -ne 0 ]; then \
		echo "  PATCH $(DESTDIR)/etc/rc.shutdown"; \
		rm -f $(DESTDIR)/etc/rc.shutdown.$$today; \
		cp -p /etc/rc.shutdown $(DESTDIR)/etc/rc.shutdown.$$today; \
		( echo "# Do not remove the 'TAG_APCCTRL' text, below"; \
		  echo "if [ -f $(PWRFAILDIR)/powerfail ]; then			     # TAG_APCCTRL";\
		  echo "    powerdown=YES					     # TAG_APCCTRL"; \
		  echo "    echo ''						     # TAG_APCCTRL"; \
		  echo "    echo 'Please ensure that the UPS has powered off before' # TAG_APCCTRL"; \
		  echo "    echo 'rebooting.  Otherwise, the UPS may cut the power'  # TAG_APCCTRL"; \
		  echo "    echo 'during the reboot.'				     # TAG_APCCTRL"; \
		  echo "    echo ''						     # TAG_APCCTRL"; \
		  echo "fi							     # TAG_APCCTRL"; \
		) >> $(DESTDIR)/etc/rc.shutdown; \
	fi; \
	grep -q /etc/rc.apcctrl /etc/rc.local; \
	if [ $$? -ne 0 ]; then \
		echo "  PATCH $(DESTDIR)/etc/rc.local"; \
		rm -f $(DESTDIR)/etc/rc.local.$$today; \
		cp -p /etc/rc.local $(DESTDIR)/etc/rc.local.$$today; \
		( echo "# Start the UPS daemon. Do not remove the 'TAG_APCCTRL' text"; \
		  echo "# if [ -x /etc/rc.apcctrl ]; then     # TAG_APCCTRL"; \
		  echo "#	  /etc/rc.apcctrl start       # TAG_APCCTRL"; \
		  echo "# fi				      # TAG_APCCTRL"; \
		) >> $(DESTDIR)/etc/rc.local; \
		echo ""; \
		( echo "While /etc/rc.local has been patched to run apcctrl,";\
		echo "the commands are currently commented out.  You should"; \
		echo "examine the $(sysconfdir)/apcctrl.conf file to ensure"; \
		echo "that it is suitable for your site.  Then run"; \
		echo "/etc/rc.apcctrl manually to ensure sane operation."; \
		echo "Once you are satisfied, uncomment the appropriate"; \
		echo "lines in /etc/rc.local" ) | /usr/bin/fmt; \
	fi

uninstall-openbsd:
	$(call DISTUNINST,OpenBSD)
	-$(call UNINST,/etc/rc.apcctrl)
	$(VV)-today="`date +%Y%m%d%H%M`"; \
	for f in $(DESTDIR)/etc/rc.local $(DESTDIR)/etc/rc.shutdown; do \
		grep -q '# TAG_APCCTRL' $$f; \
		if [ $$? -eq 0 ]; then \
			echo "  PATCH $$f"; \
			rm -f $$f.$$today; \
			cp -p $$f $$f.$$today; \
			$(SED) -e '/TAG_APCCTRL/d;' \
				< $$f.$$today > $$f; \
			chmod 644 $$f; \
		fi; \
	done
