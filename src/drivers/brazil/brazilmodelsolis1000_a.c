/*
 * BrazilModelSolis1000_a.c
 *
 *  Created on: Jun 21, 2016
 *      Author: wagner
 */

#include "apc.h"
#include "brazilmodel.h"


BrazilModelSolis1000_a::BrazilModelSolis1000_a(unsigned char model): BrazilModelSolis(model)
{
	this->CST_INPUT_FREQUENCY = 101715.0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 3.15;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 2.0;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 2.9;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 13.0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 1.4;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 18.0;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 1.44;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 13.0;
	this->CST_INPUT_VOLT_110_MULT = 1.141;
	this->CST_INPUT_VOLT_110_OFFSET = 12.0;
	this->CST_INPUT_VOLT_220_MULT = 2.5;
	this->CST_INPUT_VOLT_220_OFFSET = -250.0;
	this->CST_BATT_VOLT_MULT = 0.13908206;
	this->CST_BATT_VOLT_OFFSET = 0.6;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0.020408163;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0.1;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0.020408163;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0.1;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0.04597701;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0.15;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0.04597701;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0.15;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0.06896552;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 20.0;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0.071428575;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 25.0;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0.07770008;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 15.6;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0.08196721;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 13.0;
}

double BrazilModelSolis1000_a::getOutputActivePowerNom(){
	return 700;
}
double BrazilModelSolis1000_a::getOutputPowerNom(){
	return 1000;
}
int BrazilModelSolis1000_a::getOutputVoltageNom(){
	if(this->isOutput220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis1000_a::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis1000_a::getLineVoltageMin(){
	if(this->isLine220V()){
		return 180;
	}else{
		return 90;
	}
}
int BrazilModelSolis1000_a::getLineVoltageMax(){
	if(this->isLine220V()){
		return 250;
	}else{
		return 140;
	}
}
bool BrazilModelSolis1000_a::hasShutdownAuto(){
	return true;
}

double BrazilModelSolis1000_a::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelSolis1000_a::getBatteryCurrentNom(){
	return 14;
}
double BrazilModelSolis1000_a::getInverterEfficiency(){
	return 1.2;
}

char *BrazilModelSolis1000_a::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil Smart-UPS BR 1000VA a (code %03d)",this->vmodel);
	return name;
}



