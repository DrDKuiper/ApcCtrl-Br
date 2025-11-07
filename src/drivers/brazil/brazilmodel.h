/*
 * brazilmodels.h
 *
 *  Created on: Nov 6, 2015
 *      Author: Wagner Popov dos Santos
 *
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-400VA-115V-Brazil/P-BZ400-BR">BZ400-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-600VA-115V-Brazil/P-BZ600-BR">BZ600-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-700VA-115V-Brazil/P-BZ700-BR">BZ700-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-400VA-115V-220V-Brazil/P-BZ400BI-BR">BZ400BI-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-600VA-115V-220V-Brazil/P-BZ600BI-BR">BZ600BI-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-700VA-115V-220V-Brazil/P-BZ700BI-BR">BZ700BI-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-700-VA-Bivolt-115V-4-Outlet-Voltage-Regulation-with-Isolation-iPower-Standard/P-IPOWER700STDBI">IPOWER700STDBI</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1200-VA-Bivolt-115V-6-Outlet-Voltage-Regulation-Staytion-1200/P-STAYTION1200BI">STAYTION1200BI</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1200-115V-220V-Brazil/P-BZ1200-BR">BZ1200-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1400-VA-Bivolt-115V-6-Outlet-Voltage-Regulation-Telephone-Protection-Stay-1400/P-STAY1400BI">STAY1400BI</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1500VA-115V-220V-Brazil/P-BZ1500BI-BR">BZ1500BI-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1500VA-115V-220V-Brazil/P-BZ1500BI-BR">BZ1500BI-BR + (1)BZ24BP-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1500VA-115V-220V-Brazil/P-BZ1500PBI-BR">BZ1500PBI-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-1500VA-115V-220V-Brazil/P-BZ1500PBI-BR">BZ1500PBI-BR + (1)BZ24BP-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-2200VA-115V-220V/P-BZ2200BI-BR">BZ2200BI-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-2200VA-115V-220V/P-BZ2200BI-BR">BZ2200BI-BR + (1)BZ24BP-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-2200VA-220V/P-BZ2200I-BR">BZ2200I-BR</a>
 *		<a href="http://www.apc.com/shop/br/bz/products/APC-Back-UPS-2200VA-220V/P-BZ2200I-BR">BZ2200I-BR + (1)BZ24BP-BR</a>
 *
 */

#ifndef SRC_DRIVERS_BRAZIL_BRAZILMODEL_H_
#define SRC_DRIVERS_BRAZIL_BRAZILMODEL_H_

#include "brazilbattery.h"

enum DAYS_OF_WEEK{
	Sunday = 0,
	Monday,
	Tuesday,
	Wednesday,
	Thursday,
	Friday,
	Saturday
};

class BrazilModelAbstract
{
public:
	BrazilModelAbstract(unsigned char model);
	virtual ~BrazilModelAbstract();

	static BrazilModelAbstract *newInstance(unsigned char model, double current_expander);

	static int testBuffer(unsigned char *buffer, unsigned int datasize);

	void setBufferLock();
	void setBufferUnlock();
	void setDischarging(bool discharging, time_t* start);
	bool getDischarging();
	void refreshVariables();

	virtual int testEvents(unsigned char *buffer, unsigned int datasize) = 0;
	virtual bool setBuffer(unsigned char *buffer, unsigned int datasize) = 0;
	virtual bool setEvents(unsigned char *buffer, unsigned int datasize) = 0;
	virtual int getEventsStr(char **out) = 0;

	virtual double getTemperature() = 0;
	virtual double getLineVoltage() = 0;
	virtual double getLineFrequency() = 0;

	virtual bool isScheduleSet() = 0;

	virtual bool isScheduleSetDayOfWeek(DAYS_OF_WEEK day) = 0;
	virtual unsigned char getTurnOffHour() = 0;
	virtual unsigned char getTurnOffMinute() = 0;
	virtual unsigned char getTurnOnHour() = 0;
	virtual unsigned char getTurnOnMinute() = 0;
	virtual unsigned int getYear() = 0;
	virtual unsigned char getMonth() = 0;
	virtual unsigned char getDayOfMonth() = 0;
	virtual unsigned char getDayOfWeek() = 0;
	virtual unsigned char getHour() = 0;
	virtual unsigned char getMinute() = 0;
	virtual unsigned char getSecond() = 0;

	double getBatteryVoltage();
	double getOutputVoltage();
	double getOutputCurrent();
	double getOutputActivePower();
	double getBatteryLevel();
	double getBatteryTimeLeft();
	double getBatteryVoltageExpectedInitial();
	double getBatteryVoltageExpectedFinal();

	double getBatteryLoad();

	virtual double getInverterEfficiency() = 0;
	virtual double getBatteryVoltageNom() = 0;
	virtual double getBatteryCurrentNom() = 0;

	virtual double getOutputPowerNom() = 0;
	virtual double getOutputActivePowerNom() = 0;
	virtual int getOutputVoltageNom() = 0;
	virtual int getLineVoltageNom() = 0;
	virtual int getLineVoltageMin() = 0;
	virtual int getLineVoltageMax() = 0;

	virtual int generateCmdTurnLineOn(unsigned char **cmd, bool turnon) = 0;
	virtual int generateCmdTurnOutputOn(unsigned char **cmd, bool turnon) = 0;
	virtual int generateCmdScheduleSet(unsigned char **cmd, bool turnoff, unsigned int turnoff_minutes, bool turnon, unsigned int turnon_minutes) = 0;
	virtual int generateCmdShutdownAuto(unsigned char **cmd) = 0;
	virtual int generateCmdContinueMode(unsigned char **cmd) = 0;
	virtual int generateCmdGetEvents(unsigned char **cmd) = 0;

	virtual bool hasShutdownAuto() = 0;
	virtual char *getModelName() = 0;
	virtual bool isLineMode() = 0;
	virtual bool isBatteryMode() = 0;
	virtual bool isOverload() = 0;
	virtual bool isOverHeat() = 0;
	virtual bool isCharging() = 0;
	virtual bool isLine220V() = 0;
	virtual bool isOutput220V() = 0;
	virtual bool isOutputOn() = 0;
	virtual bool isBatteryCritical() = 0;

	double getOutputPower(){
		return this->getOutputCurrent() * this->getOutputVoltage();
	}

	double getLoadPowerPercent(){
		double out = (this->getOutputPower() / this->getOutputPowerNom())*100;
		return (out > 100 ? 100 : out);
	}
	double getLoadActivePowerPercent(){
		double out = (this->getOutputActivePower() / this->getOutputActivePowerNom())*100;
		return (out > 100 ? 100 : out);
	}

	unsigned char getModelNumber(){
		return this->vmodel;
	}

	static const unsigned int BUFFERLEN = 200;

	BrazilBattery *bat;

protected:
	virtual double getBatteryVoltageNow() = 0;
	virtual double getOutputVoltageNow() = 0;
	virtual double getOutputCurrentNow() = 0;
	virtual double getOutputActivePowerNow() = 0;

	unsigned char _buffer[BrazilModelAbstract::BUFFERLEN];
	unsigned char _events[BrazilModelAbstract::BUFFERLEN];
	unsigned int _eventssize;
	unsigned char vmodel;
	bool lock;
	int regulating_relay;

	double _batvolt[10];
	double _outvolt[10];
	double _outactpower[10];
	double _outcurrent[10];

private:
};

class BrazilModelBackUPS: public BrazilModelAbstract
{
public:
	BrazilModelBackUPS(unsigned char model);
	virtual ~BrazilModelBackUPS();

	virtual int testEvents(unsigned char *buffer, unsigned int datasize);
	virtual bool setBuffer(unsigned char *buffer, unsigned int datasize);
	virtual bool setEvents(unsigned char *buffer, unsigned int datasize);
	virtual int getEventsStr(char **out);

	virtual double getTemperature();
	virtual double getBatteryVoltageNow();
	virtual double getLineVoltage();
	virtual double getLineFrequency();
	virtual double getOutputVoltageNow();
	virtual double getOutputCurrentNow();
	virtual double getOutputActivePowerNow();

	virtual bool isScheduleSet();

	virtual bool isScheduleSetDayOfWeek(DAYS_OF_WEEK day);
	virtual unsigned char getTurnOffHour();
	virtual unsigned char getTurnOffMinute();
	virtual unsigned char getTurnOnHour();
	virtual unsigned char getTurnOnMinute();
	virtual unsigned int getYear();
	virtual unsigned char getMonth();
	virtual unsigned char getDayOfMonth();
	virtual unsigned char getDayOfWeek();
	virtual unsigned char getHour();
	virtual unsigned char getMinute();
	virtual unsigned char getSecond();

	virtual bool isLineMode();
	virtual bool isBatteryMode();
	virtual bool isOverload();
	virtual bool isOverHeat();
	virtual bool isCharging();
	virtual bool isLine220V();
	virtual bool isOutput220V();
	virtual bool isOutputOn();
	virtual bool isBatteryCritical();

	virtual int generateCmdTurnLineOn(unsigned char **cmd, bool turnon);
	virtual int generateCmdTurnOutputOn(unsigned char **cmd, bool turnon);
	virtual int generateCmdScheduleSet(unsigned char **cmd, bool turnoff, unsigned int turnoff_minutes, bool turnon, unsigned int turnon_minutes);
	virtual int generateCmdShutdownAuto(unsigned char **cmd);
	virtual int generateCmdContinueMode(unsigned char **cmd);
	virtual int generateCmdGetEvents(unsigned char **cmd);

	static const unsigned char SUBTENSAO_ENTRADA = 1;
	static const unsigned char SOBRETENSAO_ENTRADA = 2;
	static const unsigned char RETORNO_REDE_ELETRICA = 3;
	static const unsigned char SOBRECARGA = 4;
	static const unsigned char BATERIAS_DESCARREGADAS = 5;
	static const unsigned char CHAVE_LIGADA = 6;
	static const unsigned char CHAVE_DESLIGADA = 7;
	static const unsigned char DESLIGAMENTO = 8;
	static const unsigned char INICIALIZACAO = 9;
	static const unsigned char DESLIGAMENTO_REMOTO = 10;
	static const unsigned char SUPER_AQUECIMENTO = 11;
	static const unsigned char DESLIGAMENTO_PROGAMADO = 12;
	static const unsigned char ACIONAMENTO_PROGRAMADO = 13;
	static const unsigned char SAIDA_SEM_CONSUMO = 14;
	static const unsigned char BATERIA_BAIXO = 15;
	static const unsigned char SAIDA_LIGADA = 16;
	static const unsigned char BATERIAS_CARREGADAS = 17;
	static const unsigned char SHUTDOWN = 18;
	static const unsigned char FALHA_NA_COMUNICACAO = 19;
	static const unsigned char RETORNO_DE_COMUNICACAO = 20;
	static const unsigned char FALHA_NA_REDE = 21;
	static const unsigned char FALHA_AUTO_TEST = 22;
	static const unsigned char FALHA_INVERSOR = 23;
	static const unsigned char ATIVA_BYPASS = 24;
	static const unsigned char DESATIVA_BYPASS = 25;
	static const unsigned char CARREGADOR_FALHO = 26;
	static const unsigned char BATERIA_FALHA = 27;
	static const unsigned char AUTO_DIAGNOSTICO_OK = 28;
	static const unsigned char AUTO_TESTE_BAT_OK = 29;
	static const unsigned char AUTO_TESTE_BAT_BOM = 30;
	static const unsigned char AUTO_TESTE_BAT_RUIM = 31;

	static const unsigned int MSGLEN = 25;
protected:
	virtual const unsigned int getRegulatingRelay();

	double TENSAO_ENTRADA_F1;
	double TENSAO_ENTRADA_F2;
	double TENSAO_BATERIA_F1;
	double TENSAO_BATERIA_F2;
	double CORRENTE_SAIDA_F1_MR;
	double CORRENTE_SAIDA_F2_MR;
	double CORRENTE_SAIDA_F1_MI;
	double CORRENTE_SAIDA_F2_MI;
	double TENSAO_SAIDA_F1_MR[8];
	double TENSAO_SAIDA_F2_MR[8];


private:
};


class BrazilModelBackUPS1200: public BrazilModelBackUPS
{
public:
	BrazilModelBackUPS1200(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};


class BrazilModelBackUPS1500: public BrazilModelBackUPS
{
public:
	BrazilModelBackUPS1500(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelBackUPS2200: public BrazilModelBackUPS
{
public:
	BrazilModelBackUPS2200(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelBackUPS2200_22: public BrazilModelBackUPS
{
public:
	BrazilModelBackUPS2200_22(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelBackUPS700: public BrazilModelBackUPS
{
public:
	BrazilModelBackUPS700(unsigned char model);

	const unsigned int getRegulatingRelay();

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelBackUPS800: public BrazilModelBackUPS
{
public:
	BrazilModelBackUPS800(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelSolis: public BrazilModelAbstract
{
public:
	BrazilModelSolis(unsigned char model);
	virtual ~BrazilModelSolis();

	virtual int testEvents(unsigned char *buffer, unsigned int datasize);
	virtual bool setBuffer(unsigned char *buffer, unsigned int datasize);
	virtual bool setEvents(unsigned char *buffer, unsigned int datasize);
	virtual int getEventsStr(char **out);

	virtual double getTemperature();
	virtual double getBatteryVoltageNow();
	virtual double getLineVoltage();
	virtual double getLineFrequency();
	virtual double getOutputVoltageNow();
	virtual double getOutputCurrentNow();
	virtual double getOutputActivePowerNow();

	virtual bool isScheduleSet();

	virtual bool isScheduleSetDayOfWeek(DAYS_OF_WEEK day);
	virtual unsigned char getTurnOffHour();
	virtual unsigned char getTurnOffMinute();
	virtual unsigned char getTurnOnHour();
	virtual unsigned char getTurnOnMinute();
	virtual unsigned int getYear();
	virtual unsigned char getMonth();
	virtual unsigned char getDayOfMonth();
	virtual unsigned char getDayOfWeek();
	virtual unsigned char getHour();
	virtual unsigned char getMinute();
	virtual unsigned char getSecond();

	virtual bool isLineMode();
	virtual bool isBatteryMode();
	virtual bool isOverload();
	virtual bool isOverHeat();
	virtual bool isCharging();
	virtual bool isLine220V();
	virtual bool isOutput220V();
	virtual bool isOutputOn();
	virtual bool isBatteryCritical();

	virtual int generateCmdTurnLineOn(unsigned char **cmd, bool turnon);
	virtual int generateCmdTurnOutputOn(unsigned char **cmd, bool turnon);
	virtual int generateCmdScheduleSet(unsigned char **cmd, bool turnoff, unsigned int turnoff_minutes, bool turnon, unsigned int turnon_minutes);
	virtual int generateCmdShutdownAuto(unsigned char **cmd);
	virtual int generateCmdContinueMode(unsigned char **cmd);
	virtual int generateCmdGetEvents(unsigned char **cmd);

	static const unsigned int MSGLEN = 25;
protected:
	double CST_BATT_VOLT_MULT;
	double CST_BATT_VOLT_OFFSET;
	double CST_INPUT_FREQUENCY;
	double CST_INPUT_VOLT_110_MULT;
	double CST_INPUT_VOLT_110_OFFSET;
	double CST_INPUT_VOLT_220_MULT;
	double CST_INPUT_VOLT_220_OFFSET;
	double CST_OUTPUT_VOLT_220_MULT_BATT;
	double CST_OUTPUT_VOLT_220_OFFSET_BATT;
	double CST_OUTPUT_VOLT_220_MULT_LINE;
	double CST_OUTPUT_VOLT_220_OFFSET_LINE;
	double CST_OUTPUT_VOLT_110_MULT_BATT;
	double CST_OUTPUT_VOLT_110_OFFSET_BATT;
	double CST_OUTPUT_VOLT_110_MULT_LINE;
	double CST_OUTPUT_VOLT_110_OFFSET_LINE;
	double CST_OUTPUT_CURRENT_220_MULT_BATT;
	double CST_OUTPUT_CURRENT_220_OFFSET_BATT;
	double CST_OUTPUT_CURRENT_220_MULT_LINE;
	double CST_OUTPUT_CURRENT_220_OFFSET_LINE;
	double CST_OUTPUT_CURRENT_110_MULT_BATT;
	double CST_OUTPUT_CURRENT_110_OFFSET_BATT;
	double CST_OUTPUT_CURRENT_110_MULT_LINE;
	double CST_OUTPUT_CURRENT_110_OFFSET_LINE;
	double CST_OUTPUT_POWER_220_MULT_LINE;
	double CST_OUTPUT_POWER_220_OFFSET_LINE;
	double CST_OUTPUT_POWER_220_MULT_BATT;
	double CST_OUTPUT_POWER_220_OFFSET_BATT;
	double CST_OUTPUT_POWER_110_MULT_LINE;
	double CST_OUTPUT_POWER_110_OFFSET_LINE;
	double CST_OUTPUT_POWER_110_MULT_BATT;
	double CST_OUTPUT_POWER_110_OFFSET_BATT;

private:
};

class BrazilModelSolis700: public BrazilModelSolis
{
public:
	BrazilModelSolis700(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelSolis1000_a: public BrazilModelSolis
{
public:
	BrazilModelSolis1000_a(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelSolis1000_b: public BrazilModelSolis
{
public:
	BrazilModelSolis1000_b(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelSolis2000_a: public BrazilModelSolis
{
public:
	BrazilModelSolis2000_a(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelSolis2000_b: public BrazilModelSolis
{
public:
	BrazilModelSolis2000_b(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

class BrazilModelSolis3000: public BrazilModelSolis
{
public:
	BrazilModelSolis3000(unsigned char model);

	double getOutputPowerNom();
	double getOutputActivePowerNom();
	int getOutputVoltageNom();
	int getLineVoltageNom();
	int getLineVoltageMin();
	int getLineVoltageMax();
	double getBatteryVoltageNom();
	double getBatteryCurrentNom();
	double getInverterEfficiency();

	bool hasShutdownAuto();
	char *getModelName();

protected:

private:
};

#endif /* SRC_DRIVERS_BRAZIL_BRAZILMODEL_H_ */
