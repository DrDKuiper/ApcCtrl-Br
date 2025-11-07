topdir:=../..

SUBDIRS =
include $(topdir)/autoconf/targets.mak

all-install: install-gentoo
all-uninstall: uninstall-gentoo

install-gentoo:
	$(call DISTINST,gentoo)
	$(call MKDIR,/etc/init.d)
	$(call INSTPROG,744,apcctrl,/etc/init.d/apcctrl)
	$(call INSTPROG,744,halt,/etc/init.d/halt)

uninstall-gentoo:
	$(call DISTUNINST,gentoo)
	-$(call UNINST,/etc/init.d/apcctrl)
#	$(call UNINST,/etc/init.d/halt)
