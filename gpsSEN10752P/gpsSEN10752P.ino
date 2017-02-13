#include <SoftwareSerial.h>

SoftwareSerial SoftSerial(2, 3);
unsigned char buffer[64];
int count=0;   

void setup() {
  SoftSerial.begin(9600);
  Serial.begin(9600); 
}

void loop() {
  if (SoftSerial.available())                    
    {
        while(SoftSerial.available())            
        {
            buffer[count++]=SoftSerial.read();   
            if(count == 64)break;
        }
        Serial.write(buffer,count);              
        clearBufferArray();                      
        count = 0;                               
    }
}

void clearBufferArray()                     
{
    for (int i=0; i<count;i++)
    { buffer[i]=NULL;}                      
}
