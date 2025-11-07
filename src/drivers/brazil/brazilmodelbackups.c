/*
 * brazilmodels.c
 *
 *  Created on: Nov 8, 2015
 *      Author: wagner popov dos santos
 */

/*
 * Copyright (C) 2015-2016 Wagner Popov dos Santos
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of version 2 of the GNU General
 * Public License as published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1335, USA.
 */

#include <math.h>
#include <time.h>

#include "apc.h"
#include "brazilmodel.h"

BrazilModelBackUPS::BrazilModelBackUPS(unsigned char model) : BrazilModelAbstract(model){
	this->TENSAO_ENTRADA_F1 = 0;
	this->TENSAO_ENTRADA_F2 = 0;
	this->TENSAO_BATERIA_F1 = 0;
	this->TENSAO_BATERIA_F2 = 0;
	this->CORRENTE_SAIDA_F1_MR = 0;
	this->CORRENTE_SAIDA_F2_MR = 0;
	this->CORRENTE_SAIDA_F1_MI = 0;
	this->CORRENTE_SAIDA_F2_MI = 0;
	this->TENSAO_SAIDA_F1_MR[0] = 0;
	this->TENSAO_SAIDA_F1_MR[1] = 0;
	this->TENSAO_SAIDA_F1_MR[2] = 0;
	this->TENSAO_SAIDA_F1_MR[3] = 0;
	this->TENSAO_SAIDA_F1_MR[4] = 0;
	this->TENSAO_SAIDA_F1_MR[5] = 0;
	this->TENSAO_SAIDA_F1_MR[6] = 0;
	this->TENSAO_SAIDA_F1_MR[7] = 0;
	this->TENSAO_SAIDA_F2_MR[0] = 0;
	this->TENSAO_SAIDA_F2_MR[1] = 0;
	this->TENSAO_SAIDA_F2_MR[2] = 0;
	this->TENSAO_SAIDA_F2_MR[3] = 0;
	this->TENSAO_SAIDA_F2_MR[4] = 0;
	this->TENSAO_SAIDA_F2_MR[5] = 0;
	this->TENSAO_SAIDA_F2_MR[6] = 0;
	this->TENSAO_SAIDA_F2_MR[7] = 0;
}
BrazilModelBackUPS::~BrazilModelBackUPS(){
}

int BrazilModelBackUPS::testEvents(unsigned char *buffer, unsigned int datasize){
	if(buffer[0] == 172){
		if(datasize >= 5){
			if((datasize - 2) % 3 == 0){
				if((buffer[datasize-2] & 0x80) == 0x80){
					int checksum = 0;
					for(unsigned int i=1 ; i<datasize-1 ; i++){
						checksum += buffer[i];
					}
					if(checksum % 256 == buffer[datasize-1]){
						return 1;
					}else{
						return -1;
					}
				}
			}else{
			}
		}
	}else{
		return -1;
	}
	return 0;
}

bool BrazilModelBackUPS::setBuffer(unsigned char *buffer, unsigned int datasize){
	if(!this->lock){
		memcpy(this->_buffer, buffer, datasize);
		return true;
	}else{
		Dmsg(50, "warning! setBuffer with buffer locked!\n");
	}
	return false;
}

bool BrazilModelBackUPS::setEvents(unsigned char *buffer, unsigned int datasize){
	if(!this->lock){
		memcpy(this->_events, buffer, datasize);
		this->_eventssize = (datasize - 2) / 3;
		return true;
	}else{
		Dmsg(50, "warning! setEvents with buffer locked!\n");
	}
	return false;
}

int BrazilModelBackUPS::getEventsStr(char **out){
	const unsigned int EVENTSIZEMAX = 50;
	if(this->_eventssize > EVENTSIZEMAX){
		this->_eventssize = EVENTSIZEMAX;
	}

	unsigned int size = sizeof(char) * 100;  // header and footer
	size += sizeof(char) * this->_eventssize * EVENTSIZEMAX + 100;
	*out = (char*) malloc(size);

	unsigned int hour = 0;
	unsigned int minute = 0;
	unsigned int type = 0;
	unsigned int dayago = 0;
	unsigned int sizestr = 0;

	sizestr += sprintf (*out+sizestr, "\n***** events start *****\n");
	for(unsigned int i=0 ; i<this->_eventssize; i++){
		hour = (this->_events[i*3 + 1] & 0xF8) >> 3;
		minute = ((this->_events[i*3 + 1] & 0x07) << 3) | ((this->_events[i + 2] & 0xE0) >> 5);
		type = (this->_events[i*3 + 2] & 0x1F);
		dayago = (this->_events[i*3 + 3] & 0x7F);  // this.pacoteDumpping[(i * 3 + 3)] & 0x7F;

		Dmsg(99,"Event time: %02d:%02d:00\n",hour,minute);

		time_t t_day;
		time(&t_day);
		struct tm tm_day;
		gmtime_r(&t_day, &tm_day);
		tm_day.tm_mday -= dayago;
		tm_day.tm_hour = hour;
		tm_day.tm_min = minute;
		mktime(&tm_day);

		sizestr += sprintf (*out+sizestr, "%02d %04d-%02d-%02d %02d:%02d:00 UTC: ",dayago, tm_day.tm_year+1900,tm_day.tm_mon+1,tm_day.tm_mday,tm_day.tm_hour,tm_day.tm_min);

		switch (type) {
		case BrazilModelBackUPS::FALHA_NA_REDE:
			sizestr += sprintf (*out+sizestr, "LINE FAIL");
			break;
		case BrazilModelBackUPS::RETORNO_REDE_ELETRICA:
			sizestr += sprintf (*out+sizestr, "LINE RETURN");
			break;
		case BrazilModelBackUPS::CHAVE_LIGADA:
			sizestr += sprintf (*out+sizestr, "CHAVE LIGADA");
			break;
		case BrazilModelBackUPS::CHAVE_DESLIGADA:
			sizestr += sprintf (*out+sizestr, "CHAVE DESLIGADA");
			break;
		case BrazilModelBackUPS::BATERIAS_CARREGADAS:
			sizestr += sprintf (*out+sizestr, "BATTERY FULL");
			break;
		case BrazilModelBackUPS::BATERIA_FALHA:
			sizestr += sprintf (*out+sizestr, "BATTERY FAIL");
			break;
		case BrazilModelBackUPS::BATERIAS_DESCARREGADAS:
			sizestr += sprintf (*out+sizestr, "BATTERY LOW");
			break;
		case BrazilModelBackUPS::DESLIGAMENTO_PROGAMADO:
			sizestr += sprintf (*out+sizestr, "DESLIGAMENTO PROG.");
			break;
		case BrazilModelBackUPS::ACIONAMENTO_PROGRAMADO:
			sizestr += sprintf (*out+sizestr, "ACIONAMENTO PROG.");
			break;
		case BrazilModelBackUPS::DESLIGAMENTO:
			sizestr += sprintf (*out+sizestr, "UPS TURNOFF");
			break;
		case BrazilModelBackUPS::FALHA_AUTO_TEST:
			sizestr += sprintf (*out+sizestr, "AUTOTEST FAIL");
			break;
		case BrazilModelBackUPS::FALHA_INVERSOR:
			sizestr += sprintf (*out+sizestr, "INVERTER FAIL");
			break;
		case BrazilModelBackUPS::SUBTENSAO_ENTRADA:
			sizestr += sprintf (*out+sizestr, "LINE TO MANY LOW");
			break;
		case BrazilModelBackUPS::SAIDA_LIGADA:
			sizestr += sprintf (*out+sizestr, "OUT TURNON");
			break;
		case BrazilModelBackUPS::SAIDA_SEM_CONSUMO:
			sizestr += sprintf (*out+sizestr, "OUT WITHOUT LOAD");
			break;
		case BrazilModelBackUPS::SOBRETENSAO_ENTRADA:
			sizestr += sprintf (*out+sizestr, "LINE TO MANY HIGH");
			break;
		case BrazilModelBackUPS::SHUTDOWN:
			sizestr += sprintf (*out+sizestr, "UPS SHUTDOWN");
			break;
		case BrazilModelBackUPS::SOBRECARGA:
			sizestr += sprintf (*out+sizestr, "UPS OVERLOAD");
			break;
		case BrazilModelBackUPS::SUPER_AQUECIMENTO:
			sizestr += sprintf (*out+sizestr, "UPS SUPERHEAT");
			break;
		default:
			sizestr += sprintf (*out+sizestr, "unrecognized event");
		}
		sizestr += sprintf (*out+sizestr, " (%02d)",type);
		sizestr += sprintf (*out+sizestr, "\n");
	}
	sizestr += sprintf (*out+sizestr, "***** events end *****\n");
	return sizestr;
}
double BrazilModelBackUPS::getTemperature(){
	return this->_buffer[4];
}
bool BrazilModelBackUPS::isScheduleSet(){
	return (this->_buffer[17] & 0x7F) != 0; //0B01111111 = 0x7F
}
bool BrazilModelBackUPS::isScheduleSetDayOfWeek(DAYS_OF_WEEK day){
	switch(day){
	case(Sunday): 		return this->_buffer[17] & 0x01; //0B00000001;
	case(Monday): 		return this->_buffer[17] & 0x02; //0B00000010;
	case(Tuesday): 		return this->_buffer[17] & 0x04; //0B00000100;
	case(Wednesday): 	return this->_buffer[17] & 0x08; //0B00001000;
	case(Thursday): 	return this->_buffer[17] & 0x10; //0B00010000;
	case(Friday): 		return this->_buffer[17] & 0x20; //0B00100000;
	case(Saturday): 	return this->_buffer[17] & 0x40; //0B01000000;
	default:			return false;
	}
}
unsigned char BrazilModelBackUPS::getTurnOffHour(){
	return this->_buffer[15];
}
unsigned char BrazilModelBackUPS::getTurnOffMinute(){
	return this->_buffer[16];
}
unsigned char BrazilModelBackUPS::getTurnOnHour(){
	return this->_buffer[13];
}
unsigned char BrazilModelBackUPS::getTurnOnMinute(){
	return this->_buffer[14];
}
unsigned int BrazilModelBackUPS::getYear(){
	time_t now = time(NULL);
	struct tm * ptm = gmtime (&now);
	unsigned int year_read = (this->_buffer[19] & 0x0F) + 1998;
	unsigned int year_now = ptm->tm_year + 1900;
	while(year_read + 0x10 <= year_now){
		year_read += 0x10;
	}
	return year_read;
}
unsigned char BrazilModelBackUPS::getMonth(){
	return (this->_buffer[19] & 0xF0) >> 4;
}
unsigned char BrazilModelBackUPS::getDayOfMonth(){
	return (this->_buffer[18] & 0x1F);
}
unsigned char BrazilModelBackUPS::getDayOfWeek(){
	return (this->_buffer[18] & 0xE0) >> 5;
}
unsigned char BrazilModelBackUPS::getHour(){
	return this->_buffer[11];
}
unsigned char BrazilModelBackUPS::getMinute(){
	return this->_buffer[10];
}
unsigned char BrazilModelBackUPS::getSecond(){
	return this->_buffer[9];
}
double BrazilModelBackUPS::getBatteryVoltageNow(){
	return this->TENSAO_BATERIA_F1 * this->_buffer[3] + this->TENSAO_BATERIA_F2;
}
double BrazilModelBackUPS::getLineVoltage(){
	if(! this->isLineMode()){
		return 0;
	}else{
		if(! this->isLine220V()){
			return this->TENSAO_ENTRADA_F1 * this->_buffer[2] + this->TENSAO_ENTRADA_F2 - 3.0F;
		}else{
			return this->TENSAO_ENTRADA_F1 * this->_buffer[2] + this->TENSAO_ENTRADA_F2;
		}
	}
}
double BrazilModelBackUPS::getLineFrequency(){
	unsigned int freq = this->_buffer[21] + this->_buffer[22] * 256;
	freq = 0.37 * (257 - (freq >> 8));
	return freq;
}
double BrazilModelBackUPS::getOutputVoltageNow(){
	if(! this->isOutputOn()){
		return 0;
	}else{
		int index = this->getRegulatingRelay();
		if(this->isLineMode()){
			return this->TENSAO_SAIDA_F1_MR[index] * this->_buffer[1] + this->TENSAO_SAIDA_F2_MR[index];
		}else{
			return this->getOutputVoltageNom();
		}
	}
}
double BrazilModelBackUPS::getOutputCurrentNow(){
	if(this->isLineMode()){
		return  this->CORRENTE_SAIDA_F1_MR * this->_buffer[5] + this->CORRENTE_SAIDA_F2_MR;;
	}else{
		if(this->isOutputOn()){
			return this->CORRENTE_SAIDA_F1_MI * this->_buffer[5] + this->CORRENTE_SAIDA_F2_MI;
		}
	}
	return 0;
}
double BrazilModelBackUPS::getOutputActivePowerNow(){
	double pactive = this->_buffer[7] + this->_buffer[8] * 256;
	double result = 0;

	double potVA1 = 5.97 * this->getOutputPower() - 284.4;
	double potVA2 = 7.15 * this->getOutputPower() - 567.2;
	double potLin = 0.17 * pactive + 49.2;
	double potRe = 0.15 * pactive + 32.6;

	if (abs(potVA1 - pactive) < abs(potVA2 - pactive)) {
		result = potLin;
	} else {
		result = potRe;
	}
	if (result < this->getOutputPower()) {
		result = this->getOutputPower();
	}
	return result;
}
const unsigned int BrazilModelBackUPS::getRegulatingRelay(){
	int r = ((this->_buffer[6] & 0x38) >> 3);
	if(r<0 || r>7){
		Dmsg(1, "BrazilModelBackUPS error! Cannot get Regulatin Relay between 0 and 7.");
		return 4;
	}
	return r;
}
bool BrazilModelBackUPS::isCharging(){
	if(! this->isLineMode()){
		return false;
	}else{
		return ((this->_buffer[20] & 0x2) == 2);
	}
}
bool BrazilModelBackUPS::isLine220V(){
	return ((this->_buffer[20] & 0x40) == 0x40);
}
bool BrazilModelBackUPS::isOutput220V(){
	return false;
}
bool BrazilModelBackUPS::isLineMode(){
	return ((this->_buffer[20] & 0x20) != 0x20);
}
bool BrazilModelBackUPS::isBatteryMode(){
	return ! this->isLineMode();
}
bool BrazilModelBackUPS::isOutputOn(){
	return ((this->_buffer[20] & 0x08) == 0x08);
}
bool BrazilModelBackUPS::isOverload(){
	return ((this->_buffer[20] & 0x80) == 0x80);
}
bool BrazilModelBackUPS::isOverHeat(){
	return false;
}
bool BrazilModelBackUPS::isBatteryCritical(){
	return ((this->_buffer[20] & 0x04) == 0x04);
}
/*
 * To estimate the battery capacity, let's consider that the relation between voltage and time is NOT linear.
 * To do this, lets consider the voltage elevated by 4.
 */

int BrazilModelBackUPS::generateCmdTurnLineOn(unsigned char **cmd, bool turnon){
	const int size = 4;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	(*cmd)[0] = (unsigned char)204;		// cmd program turn line
	if(turnon){
		(*cmd)[1] = (unsigned char)3;			// on
		(*cmd)[2] = (unsigned char)3;
	}else{
		(*cmd)[1] = (unsigned char)4;			// off
		(*cmd)[2] = (unsigned char)4;
	}
	(*cmd)[3] = (unsigned char)(*cmd)[1] + (*cmd)[2];
	return size;
}
int BrazilModelBackUPS::generateCmdTurnOutputOn(unsigned char **cmd, bool turnon){
	const int size = 4;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	(*cmd)[0] = (unsigned char)204;		// cmd program turn output
	if(turnon){
		(*cmd)[1] = (unsigned char)1;			// on
		(*cmd)[2] = (unsigned char)1;
	}else{
		(*cmd)[1] = (unsigned char)2;			// off
		(*cmd)[2] = (unsigned char)2;
	}
	(*cmd)[3] = (unsigned char)(*cmd)[1] + (*cmd)[2];
	return size;
}
int BrazilModelBackUPS::generateCmdShutdownAuto(unsigned char **cmd){
	return 0;
//	const int size = 4;
//	*cmd = (unsigned char*) malloc(sizeof(char) * size);
//	(*cmd)[0] = (unsigned char)204;		// cmd shutdown auto start
//	(*cmd)[1] = (unsigned char)9;		// cmd shutdown auto start
//	(*cmd)[2] = (unsigned char)9;
//	(*cmd)[3] = (unsigned char)(*cmd)[1] + (*cmd)[2];
//	return size;
}
int BrazilModelBackUPS::generateCmdContinueMode(unsigned char **cmd){
	return 0;
}
int BrazilModelBackUPS::generateCmdGetEvents(unsigned char **cmd){
	const int size = 1;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	(*cmd)[0] = (unsigned char)205;		// cmd program turn line
	return size;
}
/*
 * set=true to program shutdown
 * set=false to clear shutdown programation
 */
int BrazilModelBackUPS::generateCmdScheduleSet(unsigned char **cmd, bool turnoff, unsigned int turnoff_minutes, bool turnon, unsigned int turnon_minutes)
{
	time_t now;
	time(&now);
	struct tm tm_now;

	gmtime_r(&now, &tm_now);

	Dmsg(50, "Date to send: %04d-%02d-%02d %02d:%02d:%02d %1d;\n",
			tm_now.tm_year+1900,
			tm_now.tm_mon+1,
			tm_now.tm_mday,
			tm_now.tm_hour,
			tm_now.tm_min,
			tm_now.tm_sec,
			tm_now.tm_wday
	);

	const int size = 12;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	(*cmd)[0] = (unsigned char)207;								// cmd program
	(*cmd)[1] = (unsigned char)tm_now.tm_hour;					// current hour
	(*cmd)[2] = (unsigned char)tm_now.tm_min;						// current minute
	(*cmd)[3] = (unsigned char)tm_now.tm_sec;						// current seconds
	(*cmd)[8] = (unsigned char)tm_now.tm_wday << 5;				// current day of week
	(*cmd)[8] |= ((unsigned char)tm_now.tm_mday) & 0x1F;		// 0B00011111 = 0x1F // current day oy month
	(*cmd)[9] = ((unsigned char)(tm_now.tm_mon + 1) & 0x0F) << 4;						// current month
	(*cmd)[9] |= ((unsigned char)((tm_now.tm_year - 98) % 0x10)) & 0x0F; // 0B00001111 = 0x0F  // current year
	if(turnoff || turnon){
		time_t t_turnoff,t_turnon;
		time(&t_turnoff);
		time(&t_turnon);
		if(t_turnon % 60 > 30){
			turnon_minutes++;
		}
		t_turnon += turnon_minutes * 60;
		if(t_turnoff % 60 > 30){
			turnoff_minutes++;
		}
		t_turnoff += turnoff_minutes * 60;

		t_turnoff = t_turnoff - (t_turnoff % 60);
		t_turnon = t_turnon - (t_turnon % 60);

		struct tm tm_turnoff,tm_turnon;
		gmtime_r(&t_turnoff, &tm_turnoff);
		gmtime_r(&t_turnon, &tm_turnon);

		Dmsg(50, "First date to turnoff and will be repeated all days: %04d-%02d-%02d %02d:%02d:%02d %1d;\n",
				tm_turnoff.tm_year+1900,
				tm_turnoff.tm_mon+1,
				tm_turnoff.tm_mday,
				tm_turnoff.tm_hour,
				tm_turnoff.tm_min,
				tm_turnoff.tm_sec,
				tm_turnoff.tm_wday
		);
		Dmsg(50, "First date to turnon and will be repeated all days:  %04d-%02d-%02d %02d:%02d:%02d %1d;\n",
				tm_turnon.tm_year+1900,
				tm_turnon.tm_mon+1,
				tm_turnon.tm_mday,
				tm_turnon.tm_hour,
				tm_turnon.tm_min,
				tm_turnon.tm_sec,
				tm_turnon.tm_wday
		);

		(*cmd)[4] = (unsigned char)tm_turnon.tm_hour;			// hour to turn off
		(*cmd)[5] = (unsigned char)tm_turnon.tm_min;		// minute to turn off
		(*cmd)[6] = (unsigned char)tm_turnoff.tm_hour;		// hour to turn off
		(*cmd)[7] = (unsigned char)tm_turnoff.tm_min;		// minute to turn off
		(*cmd)[10] = (unsigned char)0x7F; //0B01111111 = 0x7F	// Days of week to execute programation! All days!
	}else{
		(*cmd)[4] = (unsigned char)0;		// hour to turn on - rand number.
		(*cmd)[5] = (unsigned char)0;       // minute to turn on - rand number.
		(*cmd)[6] = (unsigned char)0;		// hour to turn off - rand number.
		(*cmd)[7] = (unsigned char)0;       // minute to turn off - rand number.
		(*cmd)[10] = 0x80; //0B10000000 = 0x80;		// none day of week to execute programation! But set auto shutdown
	}

	unsigned int checksum = 0;
	for(int i=0 ; i<11 ; i++){
		checksum += (*cmd)[i];
	}
	(*cmd)[11] = (unsigned char) checksum % 256;

	return size;
}

