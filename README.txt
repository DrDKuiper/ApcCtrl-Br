ApcCtrl é um branch do projeto apcupsd 3.14.x. Ele foi criado para dar
suporte aos nobreaks da APC no Brasil, que são modelos herdados da Microsol.
Essa aplicação não é oficial da APC Brasil. Foi desenvolvida por terceiros
e ela opera nos modelos da "GNU GENERAL PUBLIC LICENSE". Todo o código fonte
pode ser obtido no site do projeto http://www.apcctrl.com.br

Essa aplicação não exige uma máquina virtual Java. É escrita em liguagem C++
e pode ser cross-compilada para Windows.

A principal mudança em relação ao projeto original está no driver brazil. Esse
driver faz a interface entre os nobreaks da APC Brasil e a aplicação que
controla as ações em caso de eventos (falha de rede, carregamento da bateria,
etc) e os valores de tensão, corrente, agenda de desligamento e religamento,
etc.

Os modelos de nobreak da APC Brasil focados são:
  - BACK-UPS BR 1200VA (BZ1200BI-BR)
  - BACK-UPS BR 1500VA (BZ1500PBI-BR)
  - BACK-UPS BR 2200VA (BZ2200BI-BR e BZ2200I-BR)
  - SMART-UPS BR 1000VA (SUA1000BI-BR e SOLIS1000BI)
  - SMART-UPS BR 1500VA (SUA1500BI-BR e SOLIS1500BI)
  - SMART-UPS BR 2000VA (SUA2000BI-BR)
  - SMART-UPS BR 3000VA (SUA3000BI-BR)
  - STAY 800 (PS800)
  - STAY 700 (PS700)

A interface com o usuário se dá pelos executáveis:
  - apcctrl: daemon que gera as ações.
  - apctest: ferramenta de teste.
  - apcaccess: acessa os parâmetros em tempo de execução do apcctrl
  - smtp: cliente smtp simples (não é recomendado seu uso)   

Configuração:
  - /etc/apcctrl/apcctrl.conf
  
Controle de eventos:
  - /etc/apcctrl/apccontrol: controle de eventos. O daemon apcctrl chama
    esse script que pode ser alterado pelo usuário para definir como o
    servidor deve reagir para desligar, hibernar, enviar email, etc.

 

