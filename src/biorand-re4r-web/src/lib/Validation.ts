import { type ValidationResult } from './api';

export interface FormInputData {
    key: string;
    valid?: boolean;
    message?: string;
    value?: string;
}

export function validateClear(...formInputDatas: FormInputData[]) {
    return formInputDatas.map(fid => ({ ...fid, valid: undefined, message: undefined }));
}

export function validateFormInputData(vr?: ValidationResult, ...formInputDatas: FormInputData[]) {
    return formInputDatas.map(fid => {
        const validationMessage = vr ? vr[fid.key] : undefined;
        return {
            ...fid, valid: !validationMessage, message: validationMessage || ''
        }
    });
}
