/*
 * BrazilModelBackUPS1500.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"

/*
 * BrazilModelBackUPS1200.c
 *
 *  Created on: Nov 16, 2015
 *      Author: wagner
 */


#include "apc.h"
#include "brazilmodel.h"


BrazilModelBackUPS1500::BrazilModelBackUPS1500(unsigned char model) : BrazilModelBackUPS(model)
{
	this->TENSAO_ENTRADA_F1 = 1.8;
	this->TENSAO_ENTRADA_F2 = 2.2;
	this->TENSAO_BATERIA_F1 = 0.15;
	this->TENSAO_BATERIA_F2 = 0.72;
	this->CORRENTE_SAIDA_F1_MR = 0.13;
	this->CORRENTE_SAIDA_F2_MR = 0.50;
	this->CORRENTE_SAIDA_F1_MI = 0.13;
	this->CORRENTE_SAIDA_F2_MI = 0.47;
	this->TENSAO_SAIDA_F1_MR[0] = 0.93;
	this->TENSAO_SAIDA_F1_MR[1] = 0.93;
	this->TENSAO_SAIDA_F1_MR[2] = 0.93;
	this->TENSAO_SAIDA_F1_MR[3] = 0.93;
	this->TENSAO_SAIDA_F1_MR[4] = 0.93;
	this->TENSAO_SAIDA_F1_MR[5] = 0.93;
	this->TENSAO_SAIDA_F1_MR[6] = 0.93;
	this->TENSAO_SAIDA_F1_MR[7] = 0.93;
	this->TENSAO_SAIDA_F2_MR[0] = 5.1;
	this->TENSAO_SAIDA_F2_MR[1] = 5.1;
	this->TENSAO_SAIDA_F2_MR[2] = 5.1;
	this->TENSAO_SAIDA_F2_MR[3] = 5.1;
	this->TENSAO_SAIDA_F2_MR[4] = 5.1;
	this->TENSAO_SAIDA_F2_MR[5] = 5.1;
	this->TENSAO_SAIDA_F2_MR[6] = 5.1;
	this->TENSAO_SAIDA_F2_MR[7] = 5.1;
}

double BrazilModelBackUPS1500::getOutputActivePowerNom(){
	return 825;
}
double BrazilModelBackUPS1500::getOutputPowerNom(){
	return 1500;
}
int BrazilModelBackUPS1500::getOutputVoltageNom(){
	return 115;
}
int BrazilModelBackUPS1500::getLineVoltageNom(){
	if(this->isLine220V()){
		return 220;
	}else{
		return 115;
	}
}
int BrazilModelBackUPS1500::getLineVoltageMin(){
	if(this->isLine220V()){
		return 187;
	}else{
		return 97;
	}
}
int BrazilModelBackUPS1500::getLineVoltageMax(){
	if(this->isLine220V()){
		return 253;
	}else{
		return 147;
	}
}
bool BrazilModelBackUPS1500::hasShutdownAuto(){
	return true;
}

double BrazilModelBackUPS1500::getBatteryVoltageNom(){
	return 24;
}
double BrazilModelBackUPS1500::getBatteryCurrentNom(){
	return 7;
}
double BrazilModelBackUPS1500::getInverterEfficiency(){
	return 0.7;
}

char *BrazilModelBackUPS1500::getModelName(){
	static char name[MAXSTRING];
	sprintf(name, "APC Brazil BackUPS 1500 (code %03d)",this->vmodel);
	return name;
}



