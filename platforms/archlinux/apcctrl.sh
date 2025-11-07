#!/bin/bash
APCCTRL=/usr/bin/apcctrl
PKILL=/usr/bin/pkill

start() {
  echo "Iniciando o daemon APCCTRL..."
  exec $APCCTRL &
  if [ $? -eq 0 ]; then
    echo "APCCTRL inicializado com successo!"
  else
    echo "Houve um problema ao inicializar o APCCTRL."
  fi
  }

  stop() {
    echo "Interrompendo o daemon APCCTRL..."
    exec $PKILL apcctrl &
    if [ $? -eq 0 ]; then
      echo "APCCTRL finalizado com successo!"
    else
      echo "Houve um problema ao finalizar o APCCTRL."
    fi
 }

 case $1 in
      start|stop) "$1" ;;
   esac
