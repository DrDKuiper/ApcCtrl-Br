/*
 * brazilmodelsolis.c
 *
 *  Created on: July 8, 2016
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

BrazilModelSolis::BrazilModelSolis(unsigned char model) : BrazilModelAbstract(model){
	this->CST_INPUT_FREQUENCY = 0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 0;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 0;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 0;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 0;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 0;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 0;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 0;
	this->CST_INPUT_VOLT_110_MULT = 0;
	this->CST_INPUT_VOLT_110_OFFSET = 0;
	this->CST_INPUT_VOLT_220_MULT = 0;
	this->CST_INPUT_VOLT_220_OFFSET = 0;
	this->CST_BATT_VOLT_MULT = 0;
	this->CST_BATT_VOLT_OFFSET = 0;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 0;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 0;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 0;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 0;
}
BrazilModelSolis::~BrazilModelSolis(){
}

int BrazilModelSolis::testEvents(unsigned char *buffer, unsigned int datasize){
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

bool BrazilModelSolis::setBuffer(unsigned char *buffer, unsigned int datasize){
	if(!this->lock){
		memcpy(this->_buffer, buffer, datasize);
		return true;
	}else{
		Dmsg(50, "warning! setBuffer with buffer locked!\n");
	}
	return false;
}

bool BrazilModelSolis::setEvents(unsigned char *buffer, unsigned int datasize){
	if(!this->lock){
		memcpy(this->_events, buffer, datasize);
		this->_eventssize = (datasize - 2) / 3;
		return true;
	}else{
		Dmsg(50, "warning! setEvents with buffer locked!\n");
	}
	return false;
}

int BrazilModelSolis::getEventsStr(char **out){
	return 0;
}
double BrazilModelSolis::getTemperature(){
	return this->_buffer[4];
}
bool BrazilModelSolis::isScheduleSet(){
	return (this->_buffer[17] & 0x7F) != 0; //0B01111111 = 0x7F
}
bool BrazilModelSolis::isScheduleSetDayOfWeek(DAYS_OF_WEEK day){
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
unsigned char BrazilModelSolis::getTurnOffHour(){
	return this->_buffer[15];
}
unsigned char BrazilModelSolis::getTurnOffMinute(){
	return this->_buffer[16];
}
unsigned char BrazilModelSolis::getTurnOnHour(){
	return this->_buffer[13];
}
unsigned char BrazilModelSolis::getTurnOnMinute(){
	return this->_buffer[14];
}
unsigned int BrazilModelSolis::getYear(){
	time_t now = time(NULL);
	struct tm * ptm = gmtime (&now);
	unsigned int year_read = (this->_buffer[19] & 0x0F) + 1998;
	unsigned int year_now = ptm->tm_year + 1900;
	while(year_read + 0x10 <= year_now){
		year_read += 0x10;
	}
	return year_read;
}
unsigned char BrazilModelSolis::getMonth(){
	return (this->_buffer[19] & 0xF0) >> 4;
}
unsigned char BrazilModelSolis::getDayOfMonth(){
	return (this->_buffer[18] & 0x1F);
}
unsigned char BrazilModelSolis::getDayOfWeek(){
	return (this->_buffer[18] & 0xE0) >> 5;
}
unsigned char BrazilModelSolis::getHour(){
	return this->_buffer[11];
}
unsigned char BrazilModelSolis::getMinute(){
	return this->_buffer[10];
}
unsigned char BrazilModelSolis::getSecond(){
	return this->_buffer[9];
}
double BrazilModelSolis::getBatteryVoltageNow(){
	return this->CST_BATT_VOLT_MULT * this->_buffer[3] + this->CST_BATT_VOLT_OFFSET;
}
double BrazilModelSolis::getLineVoltage(){
		if(! this->isLineMode()){
			return 0;
		}else{
			if(this->isLine220V()){
				return this->CST_INPUT_VOLT_220_MULT * this->_buffer[6] + this->CST_INPUT_VOLT_220_OFFSET;
			}else{
				return this->CST_INPUT_VOLT_110_MULT * this->_buffer[6] + this->CST_INPUT_VOLT_110_OFFSET;
			}
		}
}
double BrazilModelSolis::getLineFrequency(){
	double freq = this->_buffer[21] + this->_buffer[22] * 256;
	freq = this->CST_INPUT_FREQUENCY / freq;
	return freq;
}
double BrazilModelSolis::getOutputVoltageNow(){
		if(! this->isOutputOn()){
		return 0;
	}else{
		if(this->isLineMode()){
			if(this->isOutput220V()){
				return this->CST_OUTPUT_VOLT_220_MULT_LINE * this->_buffer[1] + this->CST_OUTPUT_VOLT_220_OFFSET_LINE;
			}else{
				return this->CST_OUTPUT_VOLT_110_MULT_LINE * this->_buffer[1] + this->CST_OUTPUT_VOLT_110_OFFSET_LINE;
			}
		}else{
			return this->getOutputVoltageNom();
		}
	}
}
double BrazilModelSolis::getOutputCurrentNow(){
	if(this->isLineMode()){
		if(this->isOutput220V()){
			return  this->CST_OUTPUT_CURRENT_220_MULT_LINE * this->_buffer[5] + this->CST_OUTPUT_CURRENT_220_OFFSET_LINE;
		}else{
			return  this->CST_OUTPUT_CURRENT_110_MULT_LINE * this->_buffer[5] + this->CST_OUTPUT_CURRENT_110_OFFSET_LINE;
		}
	}else{
		if(this->isOutput220V()){
			return  this->CST_OUTPUT_CURRENT_220_MULT_BATT * this->_buffer[5] + this->CST_OUTPUT_CURRENT_220_OFFSET_BATT;
		}else{
			return  this->CST_OUTPUT_CURRENT_110_MULT_BATT * this->_buffer[5] + this->CST_OUTPUT_CURRENT_110_OFFSET_BATT;
		}
	}
}
double BrazilModelSolis::getOutputActivePowerNow(){
	double pactive = this->_buffer[7] + this->_buffer[8] * 256;
	double result = 0;
	if(this->isLineMode()){
		if(this->isOutput220V()){
			result = pactive * this->CST_OUTPUT_POWER_220_MULT_LINE + this->CST_OUTPUT_POWER_220_OFFSET_LINE;
		}else{
			result = pactive * this->CST_OUTPUT_POWER_110_MULT_LINE + this->CST_OUTPUT_POWER_110_OFFSET_LINE;
		}
	}else{
		if(this->isOutput220V()){
			result = pactive * this->CST_OUTPUT_POWER_220_MULT_BATT + this->CST_OUTPUT_POWER_220_OFFSET_BATT;
		}else{
			result = pactive * this->CST_OUTPUT_POWER_110_MULT_BATT + this->CST_OUTPUT_POWER_110_OFFSET_BATT;
		}
	}
	return result;
}
bool BrazilModelSolis::isCharging(){
	if(! this->isLineMode()){
		return false;
	}else{
		return ((this->_buffer[20] & 0x02) == 0x02);
	}
}
bool BrazilModelSolis::isLine220V(){
	return ((this->_buffer[20] & 0x40) == 0x40);
}
bool BrazilModelSolis::isOutput220V(){
	return ((this->_buffer[20] & 0x01) == 0x01);
}
bool BrazilModelSolis::isLineMode(){
	return ((this->_buffer[20] & 0x20) != 0x20);
}
bool BrazilModelSolis::isBatteryMode(){
	return ((this->_buffer[20] & 0x08) != 0x08);
}
bool BrazilModelSolis::isOutputOn(){
	return true;
}
bool BrazilModelSolis::isOverload(){
	return ((this->_buffer[20] & 0x80) == 0x80);
}
bool BrazilModelSolis::isOverHeat(){
	return ((this->_buffer[20] & 0x10) == 0x10);
}
bool BrazilModelSolis::isBatteryCritical(){
	return ((this->_buffer[20] & 0x04) == 0x04);
}
/*
 * To estimate the battery capacity, let's consider that the relation between voltage and time is NOT linear.
 * To do this, lets consider the voltage elevated by 4.
 */

int BrazilModelSolis::generateCmdTurnLineOn(unsigned char **cmd, bool turnon){
	const int size = 10;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	if(turnon){
		for(int i=0 ; i<size ; i++){
			(*cmd)[i] = (unsigned char)194;
		}
	}else{
		for(int i=0 ; i<size ; i++){
			(*cmd)[i] = (unsigned char)195;
		}
	}
	return size;
}
int BrazilModelSolis::generateCmdTurnOutputOn(unsigned char **cmd, bool turnon){
	return 0;
}
int BrazilModelSolis::generateCmdShutdownAuto(unsigned char **cmd){
	const int size = 10;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	for(int i=0 ; i<size ; i++){
		(*cmd)[i] = (unsigned char)221;
	}
	return size;
}
int BrazilModelSolis::generateCmdContinueMode(unsigned char **cmd){
	const int size = 2;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	for(int i=0 ; i<size ; i++){
		(*cmd)[i] = (unsigned char)204;
	}
	return size;
}
int BrazilModelSolis::generateCmdGetEvents(unsigned char **cmd){
	const int size = 1;
	*cmd = (unsigned char*) malloc(sizeof(char) * size);
	(*cmd)[0] = (unsigned char)205;		// cmd program turn line
	return size;
}
/*
 * set=true to program shutdown
 * set=false to clear shutdown programation
 */
int BrazilModelSolis::generateCmdScheduleSet(unsigned char **cmd, bool turnoff, unsigned int turnoff_minutes, bool turnon, unsigned int turnon_minutes)
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
		(*cmd)[10] = 0x80; //0B10000000 = 0x80;		// none day of week to execute programation. But set auto shutdown
	}

	unsigned int checksum = 0;
	for(int i=0 ; i<11 ; i++){
		checksum += (*cmd)[i];
	}
	(*cmd)[11] = (unsigned char) checksum % 256;

	return size;
}

