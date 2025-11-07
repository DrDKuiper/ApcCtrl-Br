/*
 * BrazilModelSolis2000_b.c
 *
 *  Created on: Jun 21, 2016
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"


BrazilModelSolis2000_b::BrazilModelSolis2000_b(unsigned char model): BrazilModelSolis(model)
{
	this->CST_INPUT_FREQUENCY = 101700.0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 3.0;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 12.0;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 2.93;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 13.0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 1.65;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 0.0;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 1.45;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 11.8;
	this->CST_INPUT_VOLT_110_MULT = 1.141;
	this->CST_INPUT_VOLT_110_OFFSET = 13.0;
	this->CST_INPUT_VOLT_220_MULT = 2.5;
	this->CST_INPUT_VOLT_220_OFFSET = -250.0;
	this->CST_BATT_VOLT_MULT = 0.13908206;
	this->CST_BATT_VOLT_OFFSET = 1.1;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0.043103445;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0.2;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0.043103445;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0.2;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0.08196721;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0.32;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0.08196721;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0.32;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0.06896552;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 20.0;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0.13986014;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 30.0;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0.14556041;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 23.0;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0.14285715;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 16.5;




}

double BrazilModelSolis2000_b::getOutputActivePowerNom(){
	return 1400;
}
double BrazilModelSolis2000_b::getOutputPowerNom(){
	return 2000;
}
int BrazilModelSolis2000_b::getOutputVoltageNom(){
	if(this->isOutput220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis2000_b::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis2000_b::getLineVoltageMin(){
	if(this->isLine220V()){
		return 180;
	}else{
		return 90;
	}
}
int BrazilModelSolis2000_b::getLineVoltageMax(){
	if(this->isLine220V()){
		return 250;
	}else{
		return 140;
	}
}
bool BrazilModelSolis2000_b::hasShutdownAuto(){
	return true;
}

double BrazilModelSolis2000_b::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelSolis2000_b::getBatteryCurrentNom(){
	return 18;    // 1 x 18A/h = 1 * 2.57 * 7A/h
}
double BrazilModelSolis2000_b::getInverterEfficiency(){
	return 1.2;
}

char *BrazilModelSolis2000_b::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil Smart-UPS BR 2000VA b (code %03d)",this->vmodel);
	return name;
}


