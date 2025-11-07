/*
 * BrazilModelBackUPS2200_22.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"

BrazilModelBackUPS2200_22::BrazilModelBackUPS2200_22(unsigned char model) : BrazilModelBackUPS(model)
{
	this->TENSAO_ENTRADA_F1 = 1.50;
	this->TENSAO_ENTRADA_F2 = 11.40;
	this->TENSAO_BATERIA_F1 = 0.16;
	this->TENSAO_BATERIA_F2 = -0.76;
	this->CORRENTE_SAIDA_F1_MR = 0.06;
	this->CORRENTE_SAIDA_F2_MR = 0.40;
	this->CORRENTE_SAIDA_F1_MI = 0.07;
	this->CORRENTE_SAIDA_F2_MI = 0.41;

	this->TENSAO_SAIDA_F1_MR[0] =  2.51;
	this->TENSAO_SAIDA_F1_MR[1] =  2.90;
	this->TENSAO_SAIDA_F1_MR[2] =  3.0;
	this->TENSAO_SAIDA_F1_MR[3] =  0.0;
	this->TENSAO_SAIDA_F1_MR[4] =  0.0;
	this->TENSAO_SAIDA_F1_MR[5] =  3.50;
	this->TENSAO_SAIDA_F1_MR[6] =  3.1;
	this->TENSAO_SAIDA_F1_MR[7] =  0.0;

	this->TENSAO_SAIDA_F2_MR[0] = 30.50;
	this->TENSAO_SAIDA_F2_MR[1] = 22.30;
	this->TENSAO_SAIDA_F2_MR[2] = 30.0;
	this->TENSAO_SAIDA_F2_MR[3] = 0.0;
	this->TENSAO_SAIDA_F2_MR[4] = 0.0;
	this->TENSAO_SAIDA_F2_MR[5] = -15.10;
	this->TENSAO_SAIDA_F2_MR[6] = 18.20;
	this->TENSAO_SAIDA_F2_MR[7] = 0.0;
}

double BrazilModelBackUPS2200_22::getOutputActivePowerNom(){
	return 1000;
}
double BrazilModelBackUPS2200_22::getOutputPowerNom(){
	return 2200;
}
int BrazilModelBackUPS2200_22::getOutputVoltageNom(){
	return 115;
}
int BrazilModelBackUPS2200_22::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelBackUPS2200_22::getLineVoltageMin(){
	if(this->isLine220V()){
		return 187;
	}else{
		return 97;
	}
}
int BrazilModelBackUPS2200_22::getLineVoltageMax(){
	if(this->isLine220V()){
		return 253;
	}else{
		return 147;
	}
}
bool BrazilModelBackUPS2200_22::hasShutdownAuto(){
	return true;
}

double BrazilModelBackUPS2200_22::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelBackUPS2200_22::getBatteryCurrentNom(){
	return 14;
}
double BrazilModelBackUPS2200_22::getInverterEfficiency(){
	return 0.7;
}

char *BrazilModelBackUPS2200_22::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil BackUPS 2200_22 (code %03d)",this->vmodel);
	return name;
}
