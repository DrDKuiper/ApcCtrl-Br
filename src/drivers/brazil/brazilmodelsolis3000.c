/*
 * BrazilModelSolis3000.c
 *
 *  Created on: Jun 21, 2016
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"


BrazilModelSolis3000::BrazilModelSolis3000(unsigned char model): BrazilModelSolis(model)
{
	this->CST_INPUT_FREQUENCY = 101800.0;
	this->CST_INPUT_VOLT_110_MULT = 1.127;
	this->CST_INPUT_VOLT_110_OFFSET = 12.0;
	this->CST_INPUT_VOLT_220_MULT = 2.5;
	this->CST_INPUT_VOLT_220_OFFSET = -250.0;
	this->CST_BATT_VOLT_MULT = 0.2840909;
	this->CST_BATT_VOLT_OFFSET = 0.0;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0.0625;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0.4;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0.122699395;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0.25;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0.122699395;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0.25;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0.122699395;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0.25;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 2.73;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 25.0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 2.73;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 30.0;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 1.41;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 13.0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 1.4;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 17.0;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0.20920502;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 52.0;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0.2197802;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 55.0;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0.20533882;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 19.0;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0.2197802;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 17.0;
}

double BrazilModelSolis3000::getOutputActivePowerNom(){
	return 2100;
}
double BrazilModelSolis3000::getOutputPowerNom(){
	return 3000;
}
int BrazilModelSolis3000::getOutputVoltageNom(){
	if(this->isOutput220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis3000::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis3000::getLineVoltageMin(){
	if(this->isLine220V()){
		return 180;
	}else{
		return 90;
	}
}
int BrazilModelSolis3000::getLineVoltageMax(){
	if(this->isLine220V()){
		return 250;
	}else{
		return 140;
	}
}
bool BrazilModelSolis3000::hasShutdownAuto(){
	return true;
}

double BrazilModelSolis3000::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelSolis3000::getBatteryCurrentNom(){
	return 36;    // 2 x 18A/h = 2 * 2.57 * 7A/h
}
double BrazilModelSolis3000::getInverterEfficiency(){
	return 1.2;
}

char *BrazilModelSolis3000::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil Smart-UPS BR 3000VA (code %03d)",this->vmodel);
	return name;
}


