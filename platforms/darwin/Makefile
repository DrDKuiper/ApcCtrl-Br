topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-darwin
all-uninstall: uninstall-darwin

install-darwin:
	$(call DISTINST,Darwin Mac OS X)
	$(call MKDIR,/Library/LaunchDaemons)
	$(call INSTDATA,644,org.apcctrl.apcctrl.plist,/Library/LaunchDaemons)
	-$(V)chown root:wheel $(DESTDIR)/Library/LaunchDaemons/org.apcctrl.apcctrl.plist
	$(call INSTDATA,755,apcctrl-uninstall,$(sbindir))
	$(call INSTDATA,755,apcctrl-start,$(sbindir))
	@echo "================================================="
	@echo " "
	@echo "apcctrl script installation for MacOS X (Darwin) complete." 
	@echo " "
	@echo "You should now edit $(DESTDIR)/$(sysconfdir)/apcctrl.conf to correspond"
	@echo "to your setup then start the apcctrl daemon with:"
	@echo " "
	@echo "   sudo launchctl load /Library/LaunchDaemons/org.apcctrl.apcctrl.plist"
	@echo " "
	@echo "Thereafter when you reboot, it will be stopped and started"
	@echo "automatically."
	@echo " "
	@if [ -f $(DESTDIR)/$(sysconfdir)/apcctrl ] ; then \
		echo "WARNING: Old startup script $(DESTDIR)/$(sysconfdir)/apcctrl was renamed to"; \
		echo "         $(DESTDIR)/$(sysconfdir)/apcctrl.obsolete. Be sure to remove any";   \
		echo "         references to that script that you may have manually";    \
		echo "         added to the system init scripts. apcctrl startup is";    \
		echo "         now managed via SystemStarter, making the old script";    \
		echo "         obsolete.";                                               \
		echo " ";                                                                \
		mv $(DESTDIR)/$(sysconfdir)/apcctrl $(DESTDIR)/$(sysconfdir)/apcctrl.obsolete;                 \
	fi
	@echo "================================================="

uninstall-darwin:
	$(call DISTUNINST,Darwin Mac OS X)
	-$(V)launchctl remove org.apcctrl.apcctrl
	-$(call UNINST,/System/Library/Extensions/apcctrlDummy.kext)
	-$(call UNINST,/System/Library/Extensions.mkext)
	-$(call UNINST,/System/Library/Extensions.kextcache)
	-$(call UNINST,$(sbindir)/apcctrl-uninstall)
	-$(call UNINST,$(sbindir)/apcctrl-start)
	-$(call UNINST,/Library/LaunchDaemons/org.apcctrl.apcctrl.plist)
	@echo "================================================="
	@echo " "
	@echo "Please REBOOT to complete uninstall." 
	@echo " "
	@echo "================================================="

# Root of package build sandbox
PKGROOT=/tmp/apcctrl-pkg

#
# Identity preferences for signing keys, set via Keychain Access utility or on
# command line using 'security set-identity-preference'
#
# APCCTRL_CODE_SIGNING - Code signing key
# APCCTRL_PACKAGE_SIGNING - Package signing key
#
# codesign searches by identity preference automatically
CODEKEY := APCCTRL_CODE_SIGNING
# productsign needs common name string, so look it up
PKGKEY := $(shell \
   security get-identity-preference -s APCCTRL_PACKAGE_SIGNING -c \
      | grep "common name" | cut -d\" -f2)

# Loose executables to sign
EXES := $(sbindir)/apcctrl $(sbindir)/apcaccess $(sbindir)/apctest $(sbindir)/smtp

# Make apcctrl installation package
apcctrl.pkg:
	rm -rf $(PKGROOT)
	mkdir $(PKGROOT)
	mkdir $(PKGROOT)/apcctrl
	mkdir $(PKGROOT)/apcctrl.Resources
	mkdir $(PKGROOT)/apcctrl.Scripts
	mkdir $(PKGROOT)/apcctrl.Root
	mkdir $(PKGROOT)/Apcagent.Scripts
	( cd $(topdir) && DESTDIR=$(PKGROOT)/apcctrl.Root make install )
	cp $(topdir)/ReleaseNotes $(PKGROOT)/apcctrl.Resources
	cp $(topdir)/COPYING $(PKGROOT)/apcctrl.Resources
	cp Welcome.txt $(PKGROOT)/apcctrl.Resources
	cp Conclusion.txt $(PKGROOT)/apcctrl.Resources
	cp apcctrlDaemon.preflight $(PKGROOT)/apcctrl.Scripts/preinstall
	cp apcctrlDaemon.postflight $(PKGROOT)/apcctrl.Scripts/postinstall
	cp apcagent.postinstall $(PKGROOT)/Apcagent.Scripts/postinstall
	-chown root:wheel $(PKGROOT)/apcctrl.Root/Library
	-chown root:wheel $(PKGROOT)/apcctrl.Root/Library/LaunchDaemons
	-chmod 1775 $(PKGROOT)/apcctrl.Root/Library
	-chmod 0775 $(PKGROOT)/apcctrl.Root/Library/LaunchDaemons
	-codesign --sign $(CODEKEY) --timestamp --verbose --force --prefix com.apcctrl. $(addprefix $(PKGROOT)/apcctrl.Root/,$(EXES)) && \
      codesign --verify --verbose $(addprefix $(PKGROOT)/apcctrl.Root/,$(EXES))
	-codesign --sign $(CODEKEY) --timestamp --verbose --force $(PKGROOT)/apcctrl.Root/Applications/apcagent.app && \
      codesign --verify --verbose $(PKGROOT)/apcctrl.Root/Applications/apcagent.app
	pkgbuild --root $(PKGROOT)/apcctrl.Root/Applications \
      --identifier com.apcctrl.apcagent --version $(VERSION) \
      --install-location /Applications --scripts $(PKGROOT)/Apcagent.Scripts \
      --component-plist component.plist $(PKGROOT)/apcagent.pkg
	rm -rf $(PKGROOT)/apcctrl.Root/Applications
	pkgbuild --root $(PKGROOT)/apcctrl.Root \
      --identifier com.apcctrl.apcctrl --version $(VERSION) \
      --install-location / --scripts $(PKGROOT)/apcctrl.Scripts \
      $(PKGROOT)/apcctrl.pkg
	productbuild --distribution apcctrl.dist --package-path $(PKGROOT) \
      --resources $(PKGROOT)/apcctrl.Resources --identifier org.apcctrl.apcctrl \
      --version $(VERSION) $(PKGROOT)/apcctrl-$(VERSION).pkg
	-productsign --timestamp --sign "$(PKGKEY)" $(PKGROOT)/apcctrl-$(VERSION).pkg $(PKGROOT)/apcctrl/apcctrl-$(VERSION).pkg
	hdiutil create -ov -fs HFS+ -srcfolder $(PKGROOT)/apcctrl -volname apcctrl-$(VERSION) apcctrl-$(VERSION).dmg
