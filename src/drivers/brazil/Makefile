topdir:=../../..
include $(topdir)/autoconf/targets.mak

SRCS = $(wildcard *.c)

all-targets: libbrazildrv.a

libbrazildrv.a: $(OBJS)
	$(MAKELIB)

# Include dependencies
-include $(DEPS)
