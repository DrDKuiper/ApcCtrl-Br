/*
 * BrazilModelSolis1000_b.c
 *
 *  Created on: Jun 21, 2016
 *      Author: wagner
 */

#include "apc.h"
#include "brazilmodel.h"


BrazilModelSolis1000_b::BrazilModelSolis1000_b(unsigned char model): BrazilModelSolis(model)
{
	this->CST_INPUT_FREQUENCY = 101700.0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 3.0;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 12.0;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 2.93;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 13.0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 1.465;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 16.0;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 1.465;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 12.0;
	this->CST_INPUT_VOLT_110_MULT = 2.1417;
	this->CST_INPUT_VOLT_110_OFFSET = 23.09;
	this->CST_INPUT_VOLT_220_MULT = 2.1417;
	this->CST_INPUT_VOLT_220_OFFSET = 23.09;
	this->CST_BATT_VOLT_MULT = 0.4286;
	this->CST_BATT_VOLT_OFFSET = 5.5714;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0.08237232;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0.382;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0.08237232;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0.382;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0.08620689;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0.1;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0.08620689;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0.1;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0.2378;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 178.1;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0.2664;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 148.45;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0.1227;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 68.906;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0.1367;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 52.747;
}

double BrazilModelSolis1000_b::getOutputActivePowerNom(){
	return 700;
}
double BrazilModelSolis1000_b::getOutputPowerNom(){
	return 1000;
}
int BrazilModelSolis1000_b::getOutputVoltageNom(){
	if(this->isOutput220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis1000_b::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis1000_b::getLineVoltageMin(){
	if(this->isLine220V()){
		return 180;
	}else{
		return 90;
	}
}
int BrazilModelSolis1000_b::getLineVoltageMax(){
	if(this->isLine220V()){
		return 250;
	}else{
		return 140;
	}
}
bool BrazilModelSolis1000_b::hasShutdownAuto(){
	return true;
}

double BrazilModelSolis1000_b::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelSolis1000_b::getBatteryCurrentNom(){
	return 14;
}
double BrazilModelSolis1000_b::getInverterEfficiency(){
	return 1.2;
}

char *BrazilModelSolis1000_b::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil Smart-UPS BR 1000VA b (code %03d)",this->vmodel);
	return name;
}



