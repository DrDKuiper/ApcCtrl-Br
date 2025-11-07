/*
 * BrazilModelSolis2000_a.c
 *
 *  Created on: Jun 21, 2016
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"


BrazilModelSolis2000_a::BrazilModelSolis2000_a(unsigned char model): BrazilModelSolis(model)
{
	this->CST_INPUT_FREQUENCY = 101620.0;
	this->CST_INPUT_VOLT_220_MULT = 2.5;
	this->CST_INPUT_VOLT_220_OFFSET = -250.0;
	this->CST_INPUT_VOLT_110_MULT = 1.141;
	this->CST_INPUT_VOLT_110_OFFSET = 13.0;
	this->CST_BATT_VOLT_MULT = 0.14285715;
	this->CST_BATT_VOLT_OFFSET = 0.0;
	this->CST_OUTPUT_CURRENT_220_MULT_LINE = 0.03076923;
	this->CST_OUTPUT_CURRENT_220_OFFSET_LINE = 0.0;
	this->CST_OUTPUT_CURRENT_220_MULT_BATT = 0.03076923;
	this->CST_OUTPUT_CURRENT_220_OFFSET_BATT = 0.0;
	this->CST_OUTPUT_CURRENT_110_MULT_LINE = 0.060606062;
	this->CST_OUTPUT_CURRENT_110_OFFSET_LINE = 0.1;
	this->CST_OUTPUT_CURRENT_110_MULT_BATT = 0.059523813;
	this->CST_OUTPUT_CURRENT_110_OFFSET_BATT = 0.0;
	this->CST_OUTPUT_VOLT_220_MULT_LINE = 2.8;
	this->CST_OUTPUT_VOLT_220_OFFSET_LINE = 20.0;
	this->CST_OUTPUT_VOLT_220_MULT_BATT = 2.9;
	this->CST_OUTPUT_VOLT_220_OFFSET_BATT = 18.0;
	this->CST_OUTPUT_VOLT_110_MULT_LINE = 1.375;
	this->CST_OUTPUT_VOLT_110_OFFSET_LINE = 16.0;
	this->CST_OUTPUT_VOLT_110_MULT_BATT = 1.41;
	this->CST_OUTPUT_VOLT_110_OFFSET_BATT = 16.0;
	this->CST_OUTPUT_POWER_220_MULT_LINE = 0.099009894;
	this->CST_OUTPUT_POWER_220_OFFSET_LINE = 26.0;
	this->CST_OUTPUT_POWER_220_MULT_BATT = 0.10638298;
	this->CST_OUTPUT_POWER_220_OFFSET_BATT = 30.0;
	this->CST_OUTPUT_POWER_110_MULT_LINE = 0.09803922;
	this->CST_OUTPUT_POWER_110_OFFSET_LINE = 11.0;
	this->CST_OUTPUT_POWER_110_MULT_BATT = 0.10638298;
	this->CST_OUTPUT_POWER_110_OFFSET_BATT = 15.0;
}

double BrazilModelSolis2000_a::getOutputActivePowerNom(){
	return 1400;
}
double BrazilModelSolis2000_a::getOutputPowerNom(){
	return 2000;
}
int BrazilModelSolis2000_a::getOutputVoltageNom(){
	if(this->isOutput220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis2000_a::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelSolis2000_a::getLineVoltageMin(){
	if(this->isLine220V()){
		return 180;
	}else{
		return 90;
	}
}
int BrazilModelSolis2000_a::getLineVoltageMax(){
	if(this->isLine220V()){
		return 250;
	}else{
		return 140;
	}
}
bool BrazilModelSolis2000_a::hasShutdownAuto(){
	return true;
}

double BrazilModelSolis2000_a::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelSolis2000_a::getBatteryCurrentNom(){
	return 18;    // 1 x 18A/h
}
double BrazilModelSolis2000_a::getInverterEfficiency(){
	return 1.2;
}

char *BrazilModelSolis2000_a::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil Smart-UPS BR 2000VA a (code %03d)",this->vmodel);
	return name;
}


