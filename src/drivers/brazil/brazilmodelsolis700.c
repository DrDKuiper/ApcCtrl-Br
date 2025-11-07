/*
 * brazilmodelsolis700.c
 *
 *  Created on: Jun 21, 2016
 *      Author: wagner
 */

#include "apc.h"
#include "brazilmodel.h"


BrazilModelSolis700::BrazilModelSolis700(unsigned char model): BrazilModelSolis(model)
{
	this->CST_INPUT_FREQUENCY = 101580.0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 3.15;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 2.0;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 2.9;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 13.0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 1.428;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 12.8;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 1.428;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 12.8;
	this->CST_INPUT_VOLT_110_MULT = 1.3;
	this->CST_INPUT_VOLT_110_OFFSET = 19.5;
	this->CST_INPUT_VOLT_220_MULT = 1.25;
	this->CST_INPUT_VOLT_220_OFFSET = 27.5;
	this->CST_BATT_VOLT_MULT = 0.155;
	this->CST_BATT_VOLT_OFFSET = 0.105;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0.16051364;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0.67;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0.15360983;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0.7;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0.16051364;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0.67;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0.15360983;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0.7;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0.26;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 70.0;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0.27;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 71.0;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0.26;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 70.0;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0.27;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 71.0;
}

double BrazilModelSolis700::getOutputActivePowerNom(){
	return 400;
}
double BrazilModelSolis700::getOutputPowerNom(){
	return 700;
}
int BrazilModelSolis700::getOutputVoltageNom(){
	if(this->isOutput220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis700::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis700::getLineVoltageMin(){
	if(this->isLine220V()){
		return 180;
	}else{
		return 90;
	}
}
int BrazilModelSolis700::getLineVoltageMax(){
	if(this->isLine220V()){
		return 250;
	}else{
		return 140;
	}
}
bool BrazilModelSolis700::hasShutdownAuto(){
	return true;
}

double BrazilModelSolis700::getBatteryVoltageNom(){
	return 12;
}
double BrazilModelSolis700::getBatteryCurrentNom(){
	return 7;
}
double BrazilModelSolis700::getInverterEfficiency(){
	return 1.0;
}

char *BrazilModelSolis700::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil Smart-UPS BR 700VA (code %03d)",this->vmodel);
	return name;
}



