/*
 * BrazilModelBackUPS800.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */

#include "apc.h"
#include "brazilmodel.h"


BrazilModelBackUPS800::BrazilModelBackUPS800(unsigned char model) : BrazilModelBackUPS(model)
{
	this->TENSAO_ENTRADA_F1 = 1.64;
	this->TENSAO_ENTRADA_F2 = 3.34;
	this->TENSAO_BATERIA_F1 = 0.07;
	this->TENSAO_BATERIA_F2 = 0.25;
	this->CORRENTE_SAIDA_F1_MR = 0.10;
	this->CORRENTE_SAIDA_F2_MR = 0.44;
	this->CORRENTE_SAIDA_F1_MI = 0.10;
	this->CORRENTE_SAIDA_F2_MI = 0.43;

	this->TENSAO_SAIDA_F1_MR[0] = 0.70;
	this->TENSAO_SAIDA_F1_MR[1] = 0.83;
	this->TENSAO_SAIDA_F1_MR[2] = 0.90;
	this->TENSAO_SAIDA_F1_MR[3] = 0.0;
	this->TENSAO_SAIDA_F1_MR[4] = 0.70;
	this->TENSAO_SAIDA_F1_MR[5] = 0.74;
	this->TENSAO_SAIDA_F1_MR[6] = 0.0;
	this->TENSAO_SAIDA_F1_MR[7] = 0.87;

	this->TENSAO_SAIDA_F2_MR[0] = 12.14;
	this->TENSAO_SAIDA_F2_MR[1] = 5.28;
	this->TENSAO_SAIDA_F2_MR[2] = 10.43;
	this->TENSAO_SAIDA_F2_MR[3] = 0.0;
	this->TENSAO_SAIDA_F2_MR[4] = 17.56;
	this->TENSAO_SAIDA_F2_MR[5] = 17.60;
	this->TENSAO_SAIDA_F2_MR[6] = 0.0;
	this->TENSAO_SAIDA_F2_MR[7] = 13.68;

}
double BrazilModelBackUPS800::getOutputActivePowerNom(){
	return 490;
}
double BrazilModelBackUPS800::getOutputPowerNom(){
	return 800;
}
int BrazilModelBackUPS800::getOutputVoltageNom(){
	return 115;
}
int BrazilModelBackUPS800::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelBackUPS800::getLineVoltageMin(){
	if(this->isLine220V()){
		return 187;
	}else{
		return 97;
	}
}
int BrazilModelBackUPS800::getLineVoltageMax(){
	if(this->isLine220V()){
		return 253;
	}else{
		return 147;
	}
}
bool BrazilModelBackUPS800::hasShutdownAuto(){
	return true;
}

double BrazilModelBackUPS800::getBatteryVoltageNom(){
	return 12;
}
double BrazilModelBackUPS800::getBatteryCurrentNom(){
	return 7;
}
double BrazilModelBackUPS800::getInverterEfficiency(){
	return 0.7;
}


char *BrazilModelBackUPS800::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil BackUPS 800 (code %03d)",this->vmodel);
	return name;
}

