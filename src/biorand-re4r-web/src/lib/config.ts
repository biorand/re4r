export interface ConfigOption {
    id: string;
    label: string;
    description?: string;
    type: string;
    size?: number;
    min?: number;
    max?: number;
    step?: number;
    options?: string[];
    default?: boolean | number | string;
}

export interface ConfigOptionGroup {
    label: string;
    warning?: string;
    items: ConfigOption[];
}

export interface ConfigDefinition {
    groups: ConfigOptionGroup[];
}
