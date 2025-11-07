topdir:=..

SUBDIRS = 
include $(topdir)/autoconf/targets.mak

all-targets: apcctrl.man.txt apcaccess.man.txt apctest.man.txt \
             apccontrol.man.txt apcctrl.conf.man.txt

apcctrl.man.txt: apcctrl.8
	$(call MANIFY,$<,$@)
apcaccess.man.txt: apcaccess.8
	$(call MANIFY,$<,$@)
apctest.man.txt: apctest.8
	$(call MANIFY,$<,$@)
apccontrol.man.txt: apccontrol.8
	$(call MANIFY,$<,$@)
apcctrl.conf.man.txt: apcctrl.conf.5
	$(call MANIFY,$<,$@)

all-install: install-man
all-uninstall: uninstall-man

install-man:
	$(call MKDIR,$(mandir)/man8)
	$(call INSTDATA,644,apcctrl.8,$(mandir)/man8/apcctrl.8)
	$(call INSTDATA,644,apcaccess.8,$(mandir)/man8/apcaccess.8)
	$(call INSTDATA,644,apctest.8,$(mandir)/man8/apctest.8)
	$(call INSTDATA,644,apccontrol.8,$(mandir)/man8/apccontrol.8)
	$(call MKDIR,$(mandir)/man5)
	$(call INSTDATA,644,apcctrl.conf.5,$(mandir)/man5/apcctrl.conf.5)

uninstall-man:
	$(call UNINST,$(mandir)/man8/apcctrl.8)
	$(call UNINST,$(mandir)/man8/apcaccess.8)
	$(call UNINST,$(mandir)/man8/apctest.8)
	$(call UNINST,$(mandir)/man8/apccontrol.8)
	$(call UNINST,$(mandir)/man5/apcctrl.conf.5)
