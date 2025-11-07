/*
 * BrazilModelBackUPS1200.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"


BrazilModelBackUPS1200::BrazilModelBackUPS1200(unsigned char model) : BrazilModelBackUPS(model)
{
	this->TENSAO_ENTRADA_F1 = 1.64;
	this->TENSAO_ENTRADA_F2 = 9.34;
	this->TENSAO_BATERIA_F1 = 0.15;
	this->TENSAO_BATERIA_F2 = 0.25;
	this->CORRENTE_SAIDA_F1_MR = 0.13;
	this->CORRENTE_SAIDA_F2_MR = 0.54;
	this->CORRENTE_SAIDA_F1_MI = 0.14;
	this->CORRENTE_SAIDA_F2_MI = 0.34;
	this->TENSAO_SAIDA_F1_MR[0] =  1.15;
	this->TENSAO_SAIDA_F1_MR[1] =  1.09;
	this->TENSAO_SAIDA_F1_MR[2] =  0.0;
	this->TENSAO_SAIDA_F1_MR[3] =  0.0;
	this->TENSAO_SAIDA_F1_MR[4] =  1.09;
	this->TENSAO_SAIDA_F1_MR[5] =  1.09;
	this->TENSAO_SAIDA_F1_MR[6] =  0.0;
	this->TENSAO_SAIDA_F1_MR[7] =  0.86;
	this->TENSAO_SAIDA_F2_MR[0] = -6.92;
	this->TENSAO_SAIDA_F2_MR[1] = 11.02;
	this->TENSAO_SAIDA_F2_MR[2] = 10.43;
	this->TENSAO_SAIDA_F2_MR[3] = 0.0;
	this->TENSAO_SAIDA_F2_MR[4] = -0.61;
	this->TENSAO_SAIDA_F2_MR[5] = 12.18;
	this->TENSAO_SAIDA_F2_MR[6] = 0.0;
	this->TENSAO_SAIDA_F2_MR[7] = 13.68;
}

double BrazilModelBackUPS1200::getOutputActivePowerNom(){
	return 600;
}
double BrazilModelBackUPS1200::getOutputPowerNom(){
	return 1200;
}
int BrazilModelBackUPS1200::getOutputVoltageNom(){
	return 115;
}
int BrazilModelBackUPS1200::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelBackUPS1200::getLineVoltageMin(){
	if(this->isLine220V()){
		return 187;
	}else{
		return 97;
	}
}
int BrazilModelBackUPS1200::getLineVoltageMax(){
	if(this->isLine220V()){
		return 253;
	}else{
		return 147;
	}
}
bool BrazilModelBackUPS1200::hasShutdownAuto(){
	return true;
}

double BrazilModelBackUPS1200::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelBackUPS1200::getBatteryCurrentNom(){
	return 7;
}
double BrazilModelBackUPS1200::getInverterEfficiency(){
	return 0.7;
}

char *BrazilModelBackUPS1200::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil BackUPS 1200 (code %03d)",this->vmodel);
	return name;
}
