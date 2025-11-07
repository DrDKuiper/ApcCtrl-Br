/*
 * BrazilModelBackUPS2200.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"

BrazilModelBackUPS2200::BrazilModelBackUPS2200(unsigned char model) : BrazilModelBackUPS(model)
{
	this->TENSAO_ENTRADA_F1 = 1.64;
	this->TENSAO_ENTRADA_F2 = 9.34;
	this->TENSAO_BATERIA_F1 = 0.15;
	this->TENSAO_BATERIA_F2 = 0.25;
	this->CORRENTE_SAIDA_F1_MR = 0.13;
	this->CORRENTE_SAIDA_F2_MR = 0.54;
	this->CORRENTE_SAIDA_F1_MI = 0.14;
	this->CORRENTE_SAIDA_F2_MI = 0.34;

	this->TENSAO_SAIDA_F1_MR[0] = 1.15;
	this->TENSAO_SAIDA_F1_MR[1] = 1.09;
	this->TENSAO_SAIDA_F1_MR[2] = 0.0;
	this->TENSAO_SAIDA_F1_MR[3] = 0.0;
	this->TENSAO_SAIDA_F1_MR[4] = 1.09;
	this->TENSAO_SAIDA_F1_MR[5] = 1.09;
	this->TENSAO_SAIDA_F1_MR[6] = 0.0;
	this->TENSAO_SAIDA_F1_MR[7] = 0.86;

	this->TENSAO_SAIDA_F2_MR[0] = -6.92;
	this->TENSAO_SAIDA_F2_MR[1] = 11.02;
	this->TENSAO_SAIDA_F2_MR[2] = 10.43;
	this->TENSAO_SAIDA_F2_MR[3] = 0.0;
	this->TENSAO_SAIDA_F2_MR[4] = -0.61;
	this->TENSAO_SAIDA_F2_MR[5] = 12.18;
	this->TENSAO_SAIDA_F2_MR[6] = 0.0;
	this->TENSAO_SAIDA_F2_MR[7] = 13.68;
}

double BrazilModelBackUPS2200::getOutputActivePowerNom(){
	return 1360;
}
double BrazilModelBackUPS2200::getOutputPowerNom(){
	return 2200;
}
int BrazilModelBackUPS2200::getOutputVoltageNom(){
	return 115;
}
int BrazilModelBackUPS2200::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelBackUPS2200::getLineVoltageMin(){
	if(this->isLine220V()){
		return 187;
	}else{
		return 97;
	}
}
int BrazilModelBackUPS2200::getLineVoltageMax(){
	if(this->isLine220V()){
		return 253;
	}else{
		return 147;
	}
}
bool BrazilModelBackUPS2200::hasShutdownAuto(){
	return true;
}

double BrazilModelBackUPS2200::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelBackUPS2200::getBatteryCurrentNom(){
	return 14;
}
double BrazilModelBackUPS2200::getInverterEfficiency(){
	return 0.7;
}

char *BrazilModelBackUPS2200::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil BackUPS 2200 (code %03d)",this->vmodel);
	return name;
}
